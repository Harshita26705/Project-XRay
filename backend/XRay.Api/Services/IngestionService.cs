using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Api.Services.Repositories;
using XRay.Domain.Changes;
using XRay.Domain.Graph;
using XRay.Domain.Ingestion;
using XRay.Domain.Projects;
using XRay.Infrastructure.Persistence;
using XRay.Parsers;

namespace XRay.Api.Services;

/// <summary>
/// Resolves the right <see cref="IRepositoryProvider"/> for a repository (local disk vs. Azure
/// DevOps), lists/reads its files for the requested branch, runs the C#/TypeScript/SQL parsers, and
/// materializes the result as a new current <see cref="GraphSnapshot"/> for that branch. Unresolved
/// edge targets become EXTERNAL placeholder nodes, mirroring the original Python builder's behavior.
/// </summary>
public class IngestionService
{
    private readonly AppDbContext _db;
    private readonly RepositoryProviderFactory _providerFactory;
    private readonly IConfiguration _configuration;
    private readonly CSharpParser _csharpParser = new();
    private readonly TypeScriptParser _tsParser = new();
    private readonly SqlParser _sqlParser = new();

    public IngestionService(AppDbContext db, RepositoryProviderFactory providerFactory, IConfiguration configuration)
    {
        _db = db;
        _providerFactory = providerFactory;
        _configuration = configuration;
    }

    public async Task<IngestResponse> IngestAsync(Guid projectId, IngestRequest request, CancellationToken ct = default)
    {
        var repository = await _db.Repositories.FirstOrDefaultAsync(r => r.ProjectId == projectId, ct)
                          ?? throw new InvalidOperationException("Project has no repository configured.");

        if (request.LocalRepositoryPath is not null)
        {
            repository.CloneUrl = $"file:///{request.LocalRepositoryPath.Replace('\\', '/')}";
        }

        var connection = BuildConnectionInfo(repository);
        var provider = _providerFactory.Resolve(connection.ProviderCode);

        var validation = await provider.ValidateAsync(connection, ct);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage ?? "Repository validation failed.");
        }

        var branchName = request.BranchName ?? repository.DefaultBranchName ?? "main";
        var branch = await GetOrCreateBranchAsync(repository, branchName, ct);

        var headSha = await SafeGetHeadCommitAsync(provider, connection, branchName, ct);
        if (!string.IsNullOrWhiteSpace(headSha))
        {
            var commit = await _db.Commits.FirstOrDefaultAsync(c => c.RepositoryId == repository.RepositoryId && c.CommitHash == headSha, ct);
            if (commit is null)
            {
                commit = new Commit { CommitId = Guid.NewGuid(), RepositoryId = repository.RepositoryId, CommitHash = headSha, CommittedAtUtc = DateTime.UtcNow };
                _db.Commits.Add(commit);
                await _db.SaveChangesAsync(ct);
            }
            branch.LastIndexedCommitId = commit.CommitId;
        }

        var run = new IngestionRun
        {
            IngestionRunId = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repository.RepositoryId,
            BranchId = branch.BranchId,
            TriggerType = "MANUAL",
            StartedAtUtc = DateTime.UtcNow,
            StatusCode = "RUNNING",
        };
        _db.IngestionRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        var files = await provider.ListFilesAsync(connection, branchName, ct);

        var allNodes = new Dictionary<string, ParsedNode>(StringComparer.OrdinalIgnoreCase);
        var allEdges = new List<ParsedEdge>();
        var errors = new List<string>();
        var parsedCount = 0;
        var failedCount = 0;

        foreach (var file in files)
        {
            var relative = NormalizePath(Path.GetRelativePath(rootPath, file));
            string text;
            try
            {
                text = await provider.GetFileContentAsync(connection, branchName, relative, ct);
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
                var normalizedKey = NormalizeExternalKey(node.ExternalKey);
                allNodes[normalizedKey] = node with
                {
                    ExternalKey = normalizedKey,
                    RelativeFilePath = node.RelativeFilePath is null ? null : NormalizePath(node.RelativeFilePath)
                }; // last-writer-wins merge, consistent with old builder
            }
            allEdges.AddRange(result.Edges.Select(edge => edge with
            {
                SourceExternalKey = NormalizeExternalKey(edge.SourceExternalKey),
                TargetExternalKey = NormalizeExternalKey(edge.TargetExternalKey),
                RelativeFilePath = edge.RelativeFilePath is null ? null : NormalizePath(edge.RelativeFilePath)
            }));
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

        // Retire the previous current snapshot *for this branch only* — other branches keep their
        // own current snapshot untouched (branch isolation, see Figma/update.md Workstream C).
        var previousCurrent = await _db.GraphSnapshots
            .Where(s => s.ProjectId == projectId && s.BranchId == branch.BranchId && s.IsCurrent)
            .ToListAsync(ct);
        foreach (var prev in previousCurrent) prev.IsCurrent = false;

        var snapshot = new GraphSnapshot
        {
            GraphSnapshotId = Guid.NewGuid(),
            ProjectId = projectId,
            BranchId = branch.BranchId,
            IngestionRunId = run.IngestionRunId,
            IsCurrent = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.GraphSnapshots.Add(snapshot);

        var componentTypeIds = await _db.ComponentTypes.ToDictionaryAsync(c => c.Code, c => c.Id, ct);
        var edgeTypeIds = await _db.GraphEdgeTypes.ToDictionaryAsync(e => e.Code, e => e.Id, ct);

        var nodeIdByKey = new Dictionary<string, Guid>();
        var codeFileIdByPath = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

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

    public async Task<IReadOnlyList<BranchInfo>> ListBranchesAsync(Guid projectId, CancellationToken ct = default)
    {
        var repository = await _db.Repositories.FirstOrDefaultAsync(r => r.ProjectId == projectId, ct)
                          ?? throw new InvalidOperationException("Project has no repository configured.");
        var connection = BuildConnectionInfo(repository);
        var provider = _providerFactory.Resolve(connection.ProviderCode);
        return await provider.ListBranchesAsync(connection, ct);
    }

    private async Task<Branch> GetOrCreateBranchAsync(Repository repository, string branchName, CancellationToken ct)
    {
        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.RepositoryId == repository.RepositoryId && b.Name == branchName, ct);
        if (branch is not null) return branch;

        branch = new Branch
        {
            BranchId = Guid.NewGuid(),
            RepositoryId = repository.RepositoryId,
            Name = branchName,
            IsDefault = branchName == (repository.DefaultBranchName ?? "main"),
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Branches.Add(branch);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Idempotency: a concurrent ingest/branch-list request already created this branch.
            _db.ChangeTracker.Clear();
            return await _db.Branches.FirstAsync(b => b.RepositoryId == repository.RepositoryId && b.Name == branchName, ct);
        }
        return branch;
    }

    private static async Task<string?> SafeGetHeadCommitAsync(IRepositoryProvider provider, RepositoryConnectionInfo connection, string branchName, CancellationToken ct)
    {
        try
        {
            var sha = await provider.GetHeadCommitAsync(connection, branchName, ct);
            return string.IsNullOrWhiteSpace(sha) ? null : sha;
        }
        catch
        {
            return null; // best-effort — a repo with no git history (or REST hiccup) shouldn't block ingestion
        }
    }

    private RepositoryConnectionInfo BuildConnectionInfo(Repository repository)
    {
        var cloneUrl = repository.CloneUrl ?? "";

        if (cloneUrl.Contains("_git/", StringComparison.OrdinalIgnoreCase) || cloneUrl.Contains("dev.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            var (orgUrl, project, repoName) = ParseAzureDevOpsUrl(cloneUrl);
            return new RepositoryConnectionInfo(
                "AZURE_DEVOPS",
                OrganizationUrl: orgUrl,
                ProjectName: project,
                RepositoryName: repoName,
                PersonalAccessToken: _configuration["AzureDevOps:PersonalAccessToken"]);
        }

        var localPath = ResolveLocalPath(cloneUrl)
                         ?? throw new InvalidOperationException("No local repository path or Azure DevOps URL configured for this project.");
        if (!Directory.Exists(localPath))
        {
            throw new DirectoryNotFoundException($"Repository path not found: {localPath}");
        }
        return new RepositoryConnectionInfo("LOCAL", RootPath: localPath);
    }

    /// <summary>Parses "https://dev.azure.com/{org}/{project}/_git/{repo}" into its three parts.</summary>
    private static (string OrgUrl, string Project, string Repo) ParseAzureDevOpsUrl(string cloneUrl)
    {
        var uri = new Uri(cloneUrl);
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        var gitIndex = Array.IndexOf(segments, "_git");
        if (gitIndex < 2 || gitIndex + 1 >= segments.Length)
        {
            throw new InvalidOperationException($"Could not parse Azure DevOps repository URL: {cloneUrl}");
        }
        var org = segments[0];
        var project = segments[gitIndex - 1];
        var repo = segments[gitIndex + 1];
        return ($"{uri.Scheme}://{uri.Host}/{org}", project, repo);
    }

    private static string? ResolveLocalPath(string? cloneUrl)
    {
        if (cloneUrl is null) return null;
        return cloneUrl.StartsWith("file:///", StringComparison.OrdinalIgnoreCase)
            ? cloneUrl["file:///".Length..].Replace('/', Path.DirectorySeparatorChar)
            : null;
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized;
    }

    private static string NormalizeExternalKey(string key) =>
        key.Trim().Replace('\\', '/');
}
