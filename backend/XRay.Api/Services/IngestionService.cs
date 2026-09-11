using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Graph;
using XRay.Domain.Ingestion;
using XRay.Infrastructure.Persistence;
using XRay.Parsers;

namespace XRay.Api.Services;

/// <summary>
/// Walks a local repository path (schema.md models real repo hosting; this demo ingests from disk
/// rather than cloning from Azure DevOps), runs the C#/TypeScript/SQL parsers, and materializes the
/// result as a new current <see cref="GraphSnapshot"/>. Unresolved edge targets become EXTERNAL
/// placeholder nodes, mirroring the original Python builder's behavior.
/// </summary>
public class IngestionService
{
    private static readonly string[] IgnoredSegments = { "node_modules", "bin", "obj", ".git", "dist", "build" };
    private readonly AppDbContext _db;
    private readonly CSharpParser _csharpParser = new();
    private readonly TypeScriptParser _tsParser = new();
    private readonly SqlParser _sqlParser = new();

    public IngestionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IngestResponse> IngestAsync(Guid projectId, IngestRequest request, CancellationToken ct = default)
    {
        var repository = await _db.Repositories.FirstOrDefaultAsync(r => r.ProjectId == projectId, ct)
                          ?? throw new InvalidOperationException("Project has no repository configured.");

        if (request.LocalRepositoryPath is not null)
        {
            repository.CloneUrl = $"file:///{request.LocalRepositoryPath.Replace('\\', '/')}";
        }

        var rootPath = ResolveLocalPath(repository.CloneUrl)
                       ?? throw new InvalidOperationException("No local repository path configured for this project.");
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Repository path not found: {rootPath}");
        }

        var run = new IngestionRun
        {
            IngestionRunId = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repository.RepositoryId,
            TriggerType = "MANUAL",
            StartedAtUtc = DateTime.UtcNow,
            StatusCode = "RUNNING",
        };
        _db.IngestionRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !IgnoredSegments.Any(seg => f.Replace('\\', '/').Contains($"/{seg}/", StringComparison.OrdinalIgnoreCase)))
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".ts") || f.EndsWith(".tsx") || f.EndsWith(".sql"))
            .ToList();

        var allNodes = new Dictionary<string, ParsedNode>();
        var allEdges = new List<ParsedEdge>();
        var errors = new List<string>();
        var parsedCount = 0;
        var failedCount = 0;

        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(rootPath, file).Replace('\\', '/');
            string text;
            try
            {
                text = await File.ReadAllTextAsync(file, ct);
            }
            catch (Exception ex)
            {
                errors.Add($"{relative}: {ex.Message}");
                failedCount++;
                continue;
            }

            var result = relative switch
            {
                _ when relative.EndsWith(".cs") => _csharpParser.Parse(relative, text),
                _ when relative.EndsWith(".tsx") || relative.EndsWith(".ts") => _tsParser.Parse(relative, text),
                _ when relative.EndsWith(".sql") => _sqlParser.Parse(relative, text),
                _ => new ParseResult(Array.Empty<ParsedNode>(), Array.Empty<ParsedEdge>(), Array.Empty<string>())
            };

            if (result.Errors.Count > 0)
            {
                errors.AddRange(result.Errors);
                failedCount++;
            }
            else
            {
                parsedCount++;
            }

            foreach (var node in result.Nodes)
            {
                allNodes[node.ExternalKey] = node; // last-writer-wins merge, consistent with old builder
            }
            allEdges.AddRange(result.Edges);
        }

        // Materialize unresolved edge targets/sources as EXTERNAL placeholder nodes.
        foreach (var edge in allEdges)
        {
            if (!allNodes.ContainsKey(edge.SourceExternalKey))
            {
                allNodes[edge.SourceExternalKey] = new ParsedNode(edge.SourceExternalKey, ComponentTypeCodes.External, edge.SourceExternalKey, null, null, null);
            }
            if (!allNodes.ContainsKey(edge.TargetExternalKey))
            {
                allNodes[edge.TargetExternalKey] = new ParsedNode(edge.TargetExternalKey, ComponentTypeCodes.External, edge.TargetExternalKey, null, null, null);
            }
        }

        // Retire the previous current snapshot and create a new one.
        var previousCurrent = await _db.GraphSnapshots.Where(s => s.ProjectId == projectId && s.IsCurrent).ToListAsync(ct);
        foreach (var prev in previousCurrent) prev.IsCurrent = false;

        var snapshot = new GraphSnapshot
        {
            GraphSnapshotId = Guid.NewGuid(),
            ProjectId = projectId,
            IngestionRunId = run.IngestionRunId,
            IsCurrent = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.GraphSnapshots.Add(snapshot);

        var componentTypeIds = await _db.ComponentTypes.ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        var edgeTypeIds = await _db.GraphEdgeTypes.ToDictionaryAsync(e => e.Code, e => e.Id, ct);

        var nodeIdByKey = new Dictionary<string, Guid>();
        var codeFileIdByPath = new Dictionary<string, Guid>();

        foreach (var (key, parsed) in allNodes)
        {
            Guid? codeFileId = null;
            if (parsed.RelativeFilePath is not null)
            {
                if (!codeFileIdByPath.TryGetValue(parsed.RelativeFilePath, out var existingId))
                {
                    var codeFile = await _db.CodeFiles.FirstOrDefaultAsync(
                        f => f.RepositoryId == repository.RepositoryId && f.RelativePath == parsed.RelativeFilePath, ct);
                    if (codeFile is null)
                    {
                        codeFile = new CodeFile
                        {
                            CodeFileId = Guid.NewGuid(),
                            RepositoryId = repository.RepositoryId,
                            RelativePath = parsed.RelativeFilePath,
                            FileExtension = Path.GetExtension(parsed.RelativeFilePath),
                            CreatedAtUtc = DateTime.UtcNow,
                            UpdatedAtUtc = DateTime.UtcNow,
                        };
                        _db.CodeFiles.Add(codeFile);
                    }
                    existingId = codeFile.CodeFileId;
                    codeFileIdByPath[parsed.RelativeFilePath] = existingId;
                }
                codeFileId = existingId;
            }

            var nodeId = Guid.NewGuid();
            nodeIdByKey[key] = nodeId;
            _db.GraphNodes.Add(new GraphNode
            {
                GraphNodeId = nodeId,
                GraphSnapshotId = snapshot.GraphSnapshotId,
                ComponentTypeId = componentTypeIds.GetValueOrDefault(parsed.ComponentTypeCode, componentTypeIds[ComponentTypeCodes.Unknown]),
                ExternalKey = key,
                DisplayName = parsed.DisplayName,
                CodeFileId = codeFileId,
                IsParsed = true,
                IsPartial = parsed.IsPartial,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        var edgeCreated = 0;
        var seenEdges = new HashSet<(Guid, Guid, byte)>();
        foreach (var edge in allEdges)
        {
            if (!nodeIdByKey.TryGetValue(edge.SourceExternalKey, out var sourceId)) continue;
            if (!nodeIdByKey.TryGetValue(edge.TargetExternalKey, out var targetId)) continue;
            if (sourceId == targetId) continue;
            var edgeTypeId = edgeTypeIds.GetValueOrDefault(edge.EdgeTypeCode, edgeTypeIds[GraphEdgeTypeCodes.Uses]);
            if (!seenEdges.Add((sourceId, targetId, edgeTypeId))) continue;

            Guid? sourceCodeFileId = edge.RelativeFilePath is not null && codeFileIdByPath.TryGetValue(edge.RelativeFilePath, out var cfId) ? cfId : null;

            _db.GraphEdges.Add(new GraphEdge
            {
                GraphEdgeId = Guid.NewGuid(),
                GraphSnapshotId = snapshot.GraphSnapshotId,
                GraphEdgeTypeId = edgeTypeId,
                SourceNodeId = sourceId,
                TargetNodeId = targetId,
                Confidence = edge.Confidence,
                TraversalCost = edge.EdgeTypeCode == GraphEdgeTypeCodes.Binds ? 0m : 1m,
                IsRuntimeResolved = edge.IsRuntimeResolved,
                ParserRule = edge.ParserRule,
                SourceCodeFileId = sourceCodeFileId,
                SourceLine = edge.SourceLine,
                CreatedAtUtc = DateTime.UtcNow,
            });
            edgeCreated++;
        }

        repository.LastIndexedAtUtc = DateTime.UtcNow;
        run.CompletedAtUtc = DateTime.UtcNow;
        run.StatusCode = failedCount > 0 ? "PARTIAL" : "COMPLETED";
        run.FilesDiscovered = files.Count;
        run.FilesParsed = parsedCount;
        run.FilesFailed = failedCount;

        await _db.SaveChangesAsync(ct);

        return new IngestResponse(run.IngestionRunId, files.Count, parsedCount, failedCount, allNodes.Count, edgeCreated, errors);
    }

    private static string? ResolveLocalPath(string? cloneUrl)
    {
        if (cloneUrl is null) return null;
        return cloneUrl.StartsWith("file:///", StringComparison.OrdinalIgnoreCase)
            ? cloneUrl["file:///".Length..].Replace('/', Path.DirectorySeparatorChar)
            : null;
    }
}
