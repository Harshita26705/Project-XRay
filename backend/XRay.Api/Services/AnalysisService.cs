using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Analysis;
using XRay.Domain.EvidenceModel;
using XRay.Domain.Graph;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

/// <summary>
/// The deterministic blast-radius/classifier engine. This is authoritative: risk states, distances,
/// and evidence paths come only from graph traversal + the R1-R12 rule table below (mirrors
/// schema.md section 24 / the original Python classifier 1:1). AI never participates in this
/// decision — see AiExplanationService for the strictly-explanatory layer.
/// Runs synchronously in-request rather than via the Job/worker queue for now (see repo memory:
/// "further work" — background job dispatch is a follow-up, not wired yet).
/// </summary>
public class AnalysisService
{
    private const decimal ConfidenceThreshold = 0.70m;
    private const int MaxDepth = 6;

    private readonly AppDbContext _db;
    private readonly SecurityScanService _security;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(AppDbContext db, SecurityScanService security, ILogger<AnalysisService> logger)
    {
        _db = db;
        _security = security;
        _logger = logger;
    }

    public async Task<AnalysisResponse> CreateAndRunAsync(Guid organizationId, Guid requestedByUserId, CreateAnalysisRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting analysis for change {ChangeId} requested by {UserId}", request.ChangeId, requestedByUserId);
        var change = await _db.Changes.FirstOrDefaultAsync(c => c.ChangeId == request.ChangeId, ct)
                     ?? throw new InvalidOperationException("Change not found.");

        var snapshotQuery = _db.GraphSnapshots.Where(s => s.ProjectId == change.ProjectId && s.IsCurrent);
        snapshotQuery = request.BranchName is null
            ? snapshotQuery.Where(s => s.Branch!.IsDefault || s.BranchId == null)
            : snapshotQuery.Where(s => s.Branch!.Name == request.BranchName);
        var snapshot = await snapshotQuery.FirstOrDefaultAsync(ct)
                       ?? await _db.GraphSnapshots.Where(s => s.ProjectId == change.ProjectId && s.IsCurrent).FirstOrDefaultAsync(ct);

        var queuedStatusId = await _db.AnalysisStatuses.Where(s => s.Code == "RUNNING").Select(s => s.Id).FirstAsync(ct);

        var analysis = new Analysis
        {
            AnalysisId = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = change.ProjectId,
            ChangeId = change.ChangeId,
            GraphSnapshotId = snapshot?.GraphSnapshotId,
            BranchId = snapshot?.BranchId,
            AnalysisStatusId = queuedStatusId,
            MaxDepth = MaxDepth,
            ConfidenceThreshold = ConfidenceThreshold,
            RequestedByUserId = requestedByUserId,
            StartedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Analyses.Add(analysis);

        var scope = request.Scope ?? new AnalysisScopeRequest();
        _db.AnalysisScopes.Add(new AnalysisScope
        {
            AnalysisScopeId = Guid.NewGuid(),
            AnalysisId = analysis.AnalysisId,
            ScanDirectDependencies = scope.ScanDirectDependencies,
            TraceTransitiveDependencies = scope.TraceTransitiveDependencies,
            IncludeExternalBindings = scope.IncludeExternalBindings,
            RunStaticSecurityAnalysis = scope.RunStaticSecurityAnalysis,
            CreatedAtUtc = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);

        if (snapshot is null)
        {
            analysis.AnalysisStatusId = await _db.AnalysisStatuses.Where(s => s.Code == "FAILED").Select(s => s.Id).FirstAsync(ct);
            analysis.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return await ToResponseAsync(analysis, ct);
        }

        await RunDeterministicEngineAsync(analysis, snapshot, change.ChangeId, scope.RunStaticSecurityAnalysis, ct);

        _logger.LogInformation("Completed analysis {AnalysisId}", analysis.AnalysisId);

        return await ToResponseAsync(analysis, ct);
    }

    private async Task RunDeterministicEngineAsync(Analysis analysis, GraphSnapshot snapshot, Guid changeId, bool runSecurity, CancellationToken ct)
    {
        var nodes = await _db.GraphNodes.Where(n => n.GraphSnapshotId == snapshot.GraphSnapshotId).ToListAsync(ct);
        var edges = await _db.GraphEdges.Where(e => e.GraphSnapshotId == snapshot.GraphSnapshotId).ToListAsync(ct);
        var codeFiles = await _db.CodeFiles.Where(f => nodes.Select(n => n.CodeFileId).Contains(f.CodeFileId)).ToListAsync(ct);
        var codeFileByPath = codeFiles.ToDictionary(f => f.RelativePath, f => f);

        var repository = await _db.Repositories.FirstOrDefaultAsync(r => r.ProjectId == analysis.ProjectId, ct);
        var repositoryRootPath = repository?.CloneUrl?.StartsWith("file:///", StringComparison.OrdinalIgnoreCase) == true
            ? repository.CloneUrl["file:///".Length..].Replace('/', Path.DirectorySeparatorChar)
            : null;

        var changedPaths = await _db.ChangeFiles.Where(f => f.ChangeId == changeId).Select(f => f.RelativePath).ToListAsync(ct);
        var changedNodeIds = nodes.Where(n => n.CodeFileId is not null && codeFiles.Any(f => f.CodeFileId == n.CodeFileId && changedPaths.Contains(f.RelativePath)))
            .Select(n => n.GraphNodeId).ToHashSet();

        var reachability = ComputeDeterministicReachability(edges, changedNodeIds, MaxDepth);
        var bestDistance = reachability.BestDistance;
        var bestConfidence = reachability.BestConfidence;
        var bestRuntimeResolved = reachability.BestRuntimeResolved;
        var bestPathEdges = reachability.BestPathEdges;
        var beyondDepth = reachability.BeyondDepth;

        var riskStateIds = await _db.RiskStates.ToDictionaryAsync(r => r.Code, r => r.Id, ct);
        var evidenceTypeIds = await _db.EvidenceTypes.ToDictionaryAsync(e => e.Code, e => e.Id, ct);

        var results = new List<AnalysisNodeResult>();
        var filesToScan = new List<(Guid? GraphNodeId, string? FilePath, string Content)>();

        foreach (var node in nodes)
        {
            var (riskCode, ruleCode, distance, confidence, isRuntime, isBeyond) = Classify(
                node, changedNodeIds.Contains(node.GraphNodeId), bestDistance, bestConfidence, bestRuntimeResolved, beyondDepth);

            var result = new AnalysisNodeResult
            {
                AnalysisNodeResultId = Guid.NewGuid(),
                AnalysisId = analysis.AnalysisId,
                GraphNodeId = node.GraphNodeId,
                RiskStateId = riskStateIds[riskCode],
                Distance = distance,
                MinPathConfidence = confidence,
                RuleCode = ruleCode,
                IsDirectlyChanged = changedNodeIds.Contains(node.GraphNodeId),
                IsRuntimeResolved = isRuntime,
                IsBeyondDepth = isBeyond,
                CreatedAtUtc = DateTime.UtcNow,
            };
            results.Add(result);
            _db.AnalysisNodeResults.Add(result);

            if (result.IsDirectlyChanged && node.CodeFileId is not null)
            {
                var path = codeFiles.FirstOrDefault(f => f.CodeFileId == node.CodeFileId)?.RelativePath;
                _db.Evidences.Add(new Evidence
                {
                    EvidenceId = Guid.NewGuid(),
                    AnalysisId = analysis.AnalysisId,
                    EvidenceTypeId = evidenceTypeIds["DIRECT_CHANGE"],
                    GraphNodeId = node.GraphNodeId,
                    CodeFileId = node.CodeFileId,
                    Title = $"{node.DisplayName} was directly modified",
                    Description = $"File {path} is part of this change.",
                    CreatedAtUtc = DateTime.UtcNow,
                });

                if (path is not null)
                {
                    var content = TryReadFileContent(repositoryRootPath, path);
                    if (content is not null) filesToScan.Add((node.GraphNodeId, path, content));
                }
            }
            else if (bestPathEdges.TryGetValue(node.GraphNodeId, out var pathEdges) && pathEdges.Count > 0)
            {
                var closestEdge = pathEdges[0];
                var edgeTypeCode = _db.GraphEdgeTypes.Where(t => t.Id == closestEdge.GraphEdgeTypeId).Select(t => t.Code).FirstOrDefault();
                var evidenceType = edgeTypeCode is "READS" or "WRITES" ? "DATABASE_ACCESS" : "STRUCTURAL_DEPENDENCY";
                _db.Evidences.Add(new Evidence
                {
                    EvidenceId = Guid.NewGuid(),
                    AnalysisId = analysis.AnalysisId,
                    EvidenceTypeId = evidenceTypeIds[evidenceType],
                    GraphNodeId = node.GraphNodeId,
                    GraphEdgeId = closestEdge.GraphEdgeId,
                    Title = $"{node.DisplayName} depends on a changed component",
                    Description = $"Connected via {edgeTypeCode} relationship, {pathEdges.Count} hop(s) from the change.",
                    Confidence = confidence,
                    ParserRule = closestEdge.ParserRule,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }
        }

        if (runSecurity)
        {
            var scanId = await _security.RunScanAsync(analysis.AnalysisId, filesToScan, ct);
            await AttachSecurityEvidenceAsync(analysis.AnalysisId, scanId, evidenceTypeIds["SECURITY_ALERT"], riskStateIds, results, ct);
        }

        var completedStatusId = await _db.AnalysisStatuses.Where(s => s.Code == "COMPLETED").Select(s => s.Id).FirstAsync(ct);
        analysis.AnalysisStatusId = completedStatusId;
        analysis.CompletedAtUtc = DateTime.UtcNow;
        analysis.IsDeterministicComplete = true;
        analysis.IsEvidenceComplete = true;
        analysis.OverallRiskStateId = results.Any(r => r.RiskStateId == riskStateIds["CRITICAL"]) ? riskStateIds["CRITICAL"]
            : results.Any(r => r.RiskStateId == riskStateIds["RISKY"]) ? riskStateIds["RISKY"]
            : results.Any(r => r.RiskStateId == riskStateIds["UNKNOWN"]) ? riskStateIds["UNKNOWN"]
            : riskStateIds["SAFE"];

        await _db.SaveChangesAsync(ct);
    }

    public static DeterministicReachabilityResult ComputeDeterministicReachability(IEnumerable<GraphEdge> edges, IEnumerable<Guid> changedNodeIds, int maxDepth)
    {
        var incoming = edges.GroupBy(e => e.TargetNodeId).ToDictionary(g => g.Key, g => g.OrderBy(e => e.GraphEdgeId).ToList());
        var bestDistance = new Dictionary<Guid, int>();
        var bestConfidence = new Dictionary<Guid, decimal>();
        var bestRuntimeResolved = new Dictionary<Guid, bool>();
        var bestPathEdges = new Dictionary<Guid, List<GraphEdge>>();
        var beyondDepth = new HashSet<Guid>();

        foreach (var changedNodeId in changedNodeIds.Distinct())
        {
            bestDistance[changedNodeId] = 0;
            bestConfidence[changedNodeId] = 1m;
            bestRuntimeResolved[changedNodeId] = false;
            bestPathEdges[changedNodeId] = new List<GraphEdge>();
        }

        var frontier = new Queue<(Guid NodeId, int Distance, decimal MinConfidence, bool RuntimeResolved, List<GraphEdge> Path)>();
        var bestStateByNode = new Dictionary<Guid, (int Distance, decimal MinConfidence, string PathKey)>();

        foreach (var changedNodeId in changedNodeIds.Distinct())
        {
            var seed = (changedNodeId, 0, 1m, false, new List<GraphEdge>());
            frontier.Enqueue(seed);
            bestStateByNode[changedNodeId] = (0, 1m, string.Empty);
        }

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (!bestDistance.TryGetValue(current.NodeId, out var knownDistance) || current.Distance < knownDistance)
            {
                bestDistance[current.NodeId] = current.Distance;
                bestConfidence[current.NodeId] = current.MinConfidence;
                bestRuntimeResolved[current.NodeId] = current.RuntimeResolved;
                bestPathEdges[current.NodeId] = current.Path;
            }

            if (current.Distance >= maxDepth || !incoming.TryGetValue(current.NodeId, out var dependentEdges))
            {
                continue;
            }

            foreach (var edge in dependentEdges)
            {
                var nextNodeId = edge.SourceNodeId;
                var nextDistance = current.Distance + 1;
                var nextMinConfidence = Math.Min(current.MinConfidence, edge.Confidence);
                var nextRuntimeResolved = current.RuntimeResolved || edge.IsRuntimeResolved;
                var nextPath = new List<GraphEdge>(current.Path) { edge };
                var nextPathKey = string.Join("|", nextPath.Select(e => e.GraphEdgeId));

                if (bestStateByNode.TryGetValue(nextNodeId, out var prior) &&
                    (nextDistance > prior.Distance ||
                     (nextDistance == prior.Distance && (nextMinConfidence > prior.MinConfidence ||
                        (nextMinConfidence == prior.MinConfidence && string.CompareOrdinal(nextPathKey, prior.PathKey) >= 0))))
                )
                {
                    continue;
                }

                bestStateByNode[nextNodeId] = (nextDistance, nextMinConfidence, nextPathKey);
                bestDistance[nextNodeId] = nextDistance;
                bestConfidence[nextNodeId] = nextMinConfidence;
                bestRuntimeResolved[nextNodeId] = nextRuntimeResolved;
                bestPathEdges[nextNodeId] = nextPath;
                frontier.Enqueue((nextNodeId, nextDistance, nextMinConfidence, nextRuntimeResolved, nextPath));
            }
        }

        var unboundedFrontier = new Queue<Guid>(changedNodeIds.Distinct());
        var unboundedVisited = new HashSet<Guid>(changedNodeIds.Distinct());
        while (unboundedFrontier.Count > 0)
        {
            var nodeId = unboundedFrontier.Dequeue();
            if (!incoming.TryGetValue(nodeId, out var dependentEdges)) continue;
            foreach (var edge in dependentEdges)
            {
                if (!unboundedVisited.Add(edge.SourceNodeId)) continue;
                if (!bestDistance.ContainsKey(edge.SourceNodeId)) beyondDepth.Add(edge.SourceNodeId);
                unboundedFrontier.Enqueue(edge.SourceNodeId);
            }
        }

        return new DeterministicReachabilityResult(bestDistance, bestConfidence, bestRuntimeResolved, bestPathEdges, beyondDepth);
    }

    public sealed record DeterministicReachabilityResult(
        Dictionary<Guid, int> BestDistance,
        Dictionary<Guid, decimal> BestConfidence,
        Dictionary<Guid, bool> BestRuntimeResolved,
        Dictionary<Guid, List<GraphEdge>> BestPathEdges,
        HashSet<Guid> BeyondDepth);

    private async Task AttachSecurityEvidenceAsync(Guid analysisId, Guid scanId, byte evidenceTypeId, Dictionary<string, byte> riskStateIds, List<AnalysisNodeResult> results, CancellationToken ct)
    {
        var findings = await _db.SecurityFindings.Where(f => f.SecurityScanId == scanId).ToListAsync(ct);
        var severityCodes = await _db.SecuritySeverities.ToDictionaryAsync(s => s.Id, s => s.Code, ct);
        var rules = await _db.SecurityRules.ToDictionaryAsync(r => r.SecurityRuleId, r => r, ct);

        foreach (var finding in findings)
        {
            _db.Evidences.Add(new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                AnalysisId = analysisId,
                EvidenceTypeId = evidenceTypeId,
                GraphNodeId = finding.GraphNodeId,
                Title = finding.Title,
                Description = finding.Description,
                SourceLineStart = finding.SourceLineStart,
                CreatedAtUtc = DateTime.UtcNow,
            });

            // R4: HIGH/CRITICAL security finding attributed to a node forces CRITICAL, overriding a lower classification.
            if (finding.GraphNodeId is null) continue;
            var severity = rules.TryGetValue(finding.SecurityRuleId, out var rule) ? severityCodes.GetValueOrDefault(rule.SecuritySeverityId) : null;
            if (severity is not ("CRITICAL" or "HIGH")) continue;

            var result = results.FirstOrDefault(r => r.GraphNodeId == finding.GraphNodeId);
            ApplySecurityFindingOverride(severity, result, riskStateIds["CRITICAL"]);
        }
    }

    internal static void ApplySecurityFindingOverride(string? severity, AnalysisNodeResult? result, byte criticalRiskStateId)
    {
        if (severity is not ("CRITICAL" or "HIGH") || result is null || result.RiskStateId == criticalRiskStateId) return;

        result.RiskStateId = criticalRiskStateId;
        result.RuleCode = "R4";
        result.IsSecurityAffected = true;
    }

    internal static (string RiskCode, string RuleCode, int? Distance, decimal? Confidence, bool IsRuntime, bool IsBeyond) Classify(
        GraphNode node, bool isChanged,
        Dictionary<Guid, int> bestDistance, Dictionary<Guid, decimal> bestConfidence,
        Dictionary<Guid, bool> bestRuntimeResolved, HashSet<Guid> beyondDepth)
    {
        // R1: unparsed node.
        if (!node.IsParsed) return ("UNKNOWN", "R1", null, null, false, false);

        // R2: modified + only partially parsed.
        if (isChanged && node.IsPartial) return ("UNKNOWN", "R2", 0, 1.0m, false, false);

        // R3: directly modified.
        if (isChanged) return ("CRITICAL", "R3", 0, 1.0m, false, false);

        if (!bestDistance.TryGetValue(node.GraphNodeId, out var distance))
        {
            // R11: reachable via the unbounded pass but beyond the configured depth horizon.
            if (beyondDepth.Contains(node.GraphNodeId)) return ("RISKY", "R11", null, null, false, true);
            // R12: fully parsed and provably unreachable from any changed node.
            return ("SAFE", "R12", null, null, false, false);
        }

        var confidence = bestConfidence[node.GraphNodeId];
        var isRuntime = bestRuntimeResolved[node.GraphNodeId];

        // R9: path traverses a runtime-resolved edge -> cannot be trusted deterministically.
        if (isRuntime) return ("UNKNOWN", "R9", distance, confidence, true, false);

        // R10: partially parsed non-modified node.
        if (node.IsPartial) return ("UNKNOWN", "R10", distance, confidence, false, false);

        // R5/R6: within one hop.
        if (distance <= 1)
        {
            return confidence >= ConfidenceThreshold
                ? ("CRITICAL", "R5", distance, confidence, false, false)
                : ("RISKY", "R6", distance, confidence, false, false);
        }

        // R8: indirect, within configured depth.
        return ("RISKY", "R8", distance, confidence, false, false);
    }

    private static string? TryReadFileContent(string? repositoryRootPath, string relativePath)
    {
        // Best-effort: the demo ingests from a local file:// path, so source text is re-read from disk
        // for security scanning rather than requiring separate blob storage wiring for this first pass.
        if (repositoryRootPath is null) return null;
        var fullPath = Path.Combine(repositoryRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            return File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    internal static string BuildRecommendation(string? ruleCode, bool isDirectlyChanged, int? distance, decimal? confidence)
    {
        if (isDirectlyChanged) return "Add a contract test before merging; this node is directly in the change set.";

        return ruleCode switch
        {
            "R1" => "The node could not be parsed. Re-index and validate ingestion before trusting this path.",
            "R2" => "This node was changed but only partially parsed. Treat it as a partial-data risk until the graph is refreshed.",
            "R3" => "This node was directly changed. Add a targeted regression test and verify its public contract before merge.",
            "R5" => "This is a high-risk dependency within one hop of the change. Validate the contract and behavior end-to-end.",
            "R6" => "This path is close to the change but not directly edited. Add a focused integration check for the dependency boundary.",
            "R8" => "This is an indirect dependency. Trace the path and confirm there is enough coverage for the transitive impact.",
            "R9" => "Confidence is low because this path resolves at runtime. Verify manually before shipping.",
            "R10" => "This node is partially parsed, so the path risk is uncertain. Re-check the node metadata and validate manually.",
            "R11" => "This path sits beyond the configured depth horizon. Manually verify the runtime route and any hidden dependency chains.",
            _ => distance is not null && confidence is not null
                ? $"Review the dependency path ({distance} hop(s), {confidence.Value:P0} confidence) and add a focused test to reduce risk."
                : "Review the dependency path and add a focused regression check before merging."
        };
    }

    public async Task<IReadOnlyList<AnalysisImpactEdgeResponse>> GetImpactGraphAsync(Guid analysisId, CancellationToken ct = default)
    {
        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.AnalysisId == analysisId, ct)
            ?? throw new InvalidOperationException("Analysis not found.");

        if (analysis.ChangeId is null)
        {
            return Array.Empty<AnalysisImpactEdgeResponse>();
        }

        var snapshotId = analysis.GraphSnapshotId ?? await _db.GraphSnapshots
            .Where(s => s.ProjectId == analysis.ProjectId && s.IsCurrent)
            .Select(s => s.GraphSnapshotId)
            .FirstOrDefaultAsync(ct);

        if (snapshotId == Guid.Empty)
        {
            return Array.Empty<AnalysisImpactEdgeResponse>();
        }

        var nodes = await _db.GraphNodes.Where(n => n.GraphSnapshotId == snapshotId).ToListAsync(ct);
        var edges = await _db.GraphEdges.Where(e => e.GraphSnapshotId == snapshotId).ToListAsync(ct);
        var areaFiles = await _db.ChangeFiles.Where(f => f.ChangeId == analysis.ChangeId.Value).Select(f => f.RelativePath).ToListAsync(ct);

        var changedNodeIds = nodes
            .Where(n => n.CodeFileId is not null)
            .Join(
                await _db.CodeFiles.Where(f => f.RepositoryId == _db.Repositories.Where(r => r.ProjectId == analysis.ProjectId).Select(r => r.RepositoryId).FirstOrDefault()).ToListAsync(ct),
                n => n.CodeFileId,
                f => f.CodeFileId,
                (n, f) => new { Node = n, Path = f.RelativePath })
            .Where(x => areaFiles.Contains(x.Path))
            .Select(x => x.Node.GraphNodeId)
            .ToHashSet();

        if (changedNodeIds.Count == 0)
        {
            changedNodeIds = (await _db.AnalysisNodeResults
                .Where(r => r.AnalysisId == analysisId)
                .Select(r => r.GraphNodeId)
                .Distinct()
                .ToListAsync(ct))
                .ToHashSet();
        }

        var reachability = ComputeDeterministicReachability(edges, changedNodeIds, MaxDepth);
        var edgeTypeCodes = await _db.GraphEdgeTypes.ToDictionaryAsync(e => e.Id, e => e.Code, ct);

        return reachability.BestPathEdges
            .SelectMany(kvp => kvp.Value)
            .GroupBy(edge => new { edge.SourceNodeId, edge.TargetNodeId, edge.GraphEdgeTypeId, edge.Confidence })
            .Select(g => new AnalysisImpactEdgeResponse(
                g.Key.SourceNodeId,
                g.Key.TargetNodeId,
                edgeTypeCodes.GetValueOrDefault(g.Key.GraphEdgeTypeId, "USES"),
                g.Key.Confidence))
            .OrderBy(e => e.SourceNodeId)
            .ThenBy(e => e.TargetNodeId)
            .ToList();
    }

    public async Task<AnalysisResponse?> GetAsync(Guid analysisId, CancellationToken ct = default)
    {
        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.AnalysisId == analysisId, ct);
        return analysis is null ? null : await ToResponseAsync(analysis, ct);
    }

    public async Task<IReadOnlyList<AnalysisResponse>> ListAsync(Guid projectId, CancellationToken ct = default)
    {
        var analyses = await _db.Analyses.Where(a => a.ProjectId == projectId).OrderByDescending(a => a.CreatedAtUtc).ToListAsync(ct);
        var result = new List<AnalysisResponse>();
        foreach (var a in analyses) result.Add(await ToResponseAsync(a, ct));
        return result;
    }

    private async Task<AnalysisResponse> ToResponseAsync(Analysis analysis, CancellationToken ct)
    {
        var componentTypes = await _db.ComponentTypes.ToDictionaryAsync(c => c.Id, c => c.Code, ct);
        var riskStates = await _db.RiskStates.ToDictionaryAsync(r => r.Id, r => r.Code, ct);
        var statuses = await _db.AnalysisStatuses.ToDictionaryAsync(s => s.Id, s => s.Code, ct);

        var nodeResults = await _db.AnalysisNodeResults.Where(n => n.AnalysisId == analysis.AnalysisId).ToListAsync(ct);
        var graphNodes = await _db.GraphNodes.Where(n => nodeResults.Select(r => r.GraphNodeId).Contains(n.GraphNodeId)).ToListAsync(ct);
        var graphNodeById = graphNodes.ToDictionary(n => n.GraphNodeId);

        var change = analysis.ChangeId is null ? null : await _db.Changes.FirstOrDefaultAsync(c => c.ChangeId == analysis.ChangeId, ct);
        var branchName = analysis.BranchId is null ? null : await _db.Branches.Where(b => b.BranchId == analysis.BranchId).Select(b => b.Name).FirstOrDefaultAsync(ct);

        var nodeDtos = nodeResults.Select(r =>
        {
            graphNodeById.TryGetValue(r.GraphNodeId, out var gn);
            var recommendation = BuildRecommendation(r.RuleCode, r.IsDirectlyChanged, r.Distance, r.MinPathConfidence);
            return new AnalysisNodeResultResponse(
                r.GraphNodeId, gn?.DisplayName ?? "(unknown)", gn is null ? "UNKNOWN" : componentTypes.GetValueOrDefault(gn.ComponentTypeId, "UNKNOWN"),
                riskStates.GetValueOrDefault(r.RiskStateId, "UNKNOWN"), r.Distance, r.MinPathConfidence, r.RuleCode, r.IsDirectlyChanged, recommendation);
        }).ToList();

        var edges = await GetImpactGraphAsync(analysis.AnalysisId, ct);

        return new AnalysisResponse(
            analysis.AnalysisId, analysis.ChangeId ?? Guid.Empty, change?.Title ?? "(manual analysis)",
            statuses.GetValueOrDefault(analysis.AnalysisStatusId, "QUEUED"),
            analysis.OverallRiskStateId is null ? null : riskStates.GetValueOrDefault(analysis.OverallRiskStateId.Value),
            branchName,
            nodeDtos.Count(n => n.RiskState == "CRITICAL"),
            nodeDtos.Count(n => n.RiskState == "RISKY"),
            nodeDtos.Count(n => n.RiskState == "SAFE"),
            nodeDtos.Count(n => n.RiskState == "UNKNOWN"),
            analysis.CreatedAtUtc, analysis.CompletedAtUtc, edges, nodeDtos);
    }
}
