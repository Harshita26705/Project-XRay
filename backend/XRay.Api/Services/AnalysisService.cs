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

    public AnalysisService(AppDbContext db, SecurityScanService security)
    {
        _db = db;
        _security = security;
    }

    public async Task<AnalysisResponse> CreateAndRunAsync(Guid organizationId, Guid requestedByUserId, CreateAnalysisRequest request, CancellationToken ct = default)
    {
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

        // Reverse adjacency: who depends on X (i.e. edges pointing INTO X), so we can walk "dependents of the change".
        var incoming = edges.GroupBy(e => e.TargetNodeId).ToDictionary(g => g.Key, g => g.ToList());

        var bestDistance = new Dictionary<Guid, int>();
        var bestConfidence = new Dictionary<Guid, decimal>();
        var bestRuntimeResolved = new Dictionary<Guid, bool>();
        var bestPathEdges = new Dictionary<Guid, List<GraphEdge>>();
        var beyondDepth = new HashSet<Guid>();

        // BFS from each changed node, walking backwards along incoming edges (dependents).
        var frontier = new Queue<(Guid NodeId, int Distance, decimal MinConfidence, bool RuntimeResolved, List<GraphEdge> Path)>();
        foreach (var changedId in changedNodeIds)
        {
            frontier.Enqueue((changedId, 0, 1.0m, false, new List<GraphEdge>()));
        }

        var visited = new HashSet<Guid>(changedNodeIds);
        var unboundedVisited = new HashSet<Guid>(changedNodeIds);

        while (frontier.Count > 0)
        {
            var (nodeId, distance, minConf, runtime, path) = frontier.Dequeue();

            if (!changedNodeIds.Contains(nodeId))
            {
                if (!bestDistance.TryGetValue(nodeId, out var existingDist) || distance < existingDist)
                {
                    bestDistance[nodeId] = distance;
                    bestConfidence[nodeId] = minConf;
                    bestRuntimeResolved[nodeId] = runtime;
                    bestPathEdges[nodeId] = path;
                }
            }

            if (distance >= MaxDepth) continue;
            if (!incoming.TryGetValue(nodeId, out var dependentEdges)) continue;

            foreach (var edge in dependentEdges)
            {
                if (visited.Contains(edge.SourceNodeId) && bestDistance.TryGetValue(edge.SourceNodeId, out var d) && d <= distance + 1) continue;
                visited.Add(edge.SourceNodeId);
                var newPath = new List<GraphEdge>(path) { edge };
                frontier.Enqueue((edge.SourceNodeId, distance + 1, Math.Min(minConf, edge.Confidence), runtime || edge.IsRuntimeResolved, newPath));
            }
        }

        // Unbounded pass (no depth cap) to find nodes that are reachable but beyond MaxDepth (R11).
        var unboundedFrontier = new Queue<Guid>(changedNodeIds);
        while (unboundedFrontier.Count > 0)
        {
            var nodeId = unboundedFrontier.Dequeue();
            if (!incoming.TryGetValue(nodeId, out var dependentEdges)) continue;
            foreach (var edge in dependentEdges)
            {
                if (unboundedVisited.Add(edge.SourceNodeId))
                {
                    if (!bestDistance.ContainsKey(edge.SourceNodeId)) beyondDepth.Add(edge.SourceNodeId);
                    unboundedFrontier.Enqueue(edge.SourceNodeId);
                }
            }
        }

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

            // Evidence: direct change
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
            if (result is not null && result.RiskStateId != riskStateIds["CRITICAL"])
            {
                result.RiskStateId = riskStateIds["CRITICAL"];
                result.RuleCode = "R4";
                result.IsSecurityAffected = true;
            }
        }
    }

    private static (string RiskCode, string RuleCode, int? Distance, decimal? Confidence, bool IsRuntime, bool IsBeyond) Classify(
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
            return new AnalysisNodeResultResponse(
                r.GraphNodeId, gn?.DisplayName ?? "(unknown)", gn is null ? "UNKNOWN" : componentTypes.GetValueOrDefault(gn.ComponentTypeId, "UNKNOWN"),
                riskStates.GetValueOrDefault(r.RiskStateId, "UNKNOWN"), r.Distance, r.MinPathConfidence, r.RuleCode, r.IsDirectlyChanged);
        }).ToList();

        return new AnalysisResponse(
            analysis.AnalysisId, analysis.ChangeId ?? Guid.Empty, change?.Title ?? "(manual analysis)",
            statuses.GetValueOrDefault(analysis.AnalysisStatusId, "QUEUED"),
            analysis.OverallRiskStateId is null ? null : riskStates.GetValueOrDefault(analysis.OverallRiskStateId.Value),
            branchName,
            nodeDtos.Count(n => n.RiskState == "CRITICAL"),
            nodeDtos.Count(n => n.RiskState == "RISKY"),
            nodeDtos.Count(n => n.RiskState == "SAFE"),
            nodeDtos.Count(n => n.RiskState == "UNKNOWN"),
            analysis.CreatedAtUtc, analysis.CompletedAtUtc, nodeDtos);
    }
}
