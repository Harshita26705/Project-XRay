using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Api.Services;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectService _projects;
    private readonly IngestionService _ingestion;
    private readonly ICurrentUserService _currentUser;
    private readonly AppDbContext _db;
    private readonly AiExplanationService _ai;

    public ProjectsController(ProjectService projects, IngestionService ingestion, ICurrentUserService currentUser, AppDbContext db, AiExplanationService ai)
    {
        _projects = projects;
        _ingestion = ingestion;
        _currentUser = currentUser;
        _db = db;
        _ai = ai;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectResponse>>> List(CancellationToken ct)
    {
        var org = await ResolveOrganizationAsync(ct);
        return Ok(await _projects.ListAsync(org, ct));
    }

    [HttpGet("{projectId:guid}")]
    public async Task<ActionResult<ProjectResponse>> Get(Guid projectId, CancellationToken ct)
    {
        var project = await _projects.GetAsync(projectId, ct);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetOrProvisionUserAsync(User, ct);
        var org = await _currentUser.GetOrProvisionOrganizationAsync(user, ct);
        var created = await _projects.CreateAsync(org.OrganizationId, user.UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { projectId = created.ProjectId }, created);
    }

    [HttpGet("{projectId:guid}/branches")]
    public async Task<ActionResult<IReadOnlyList<BranchResponse>>> ListBranches(Guid projectId, CancellationToken ct)
    {
        try
        {
            var branches = await _ingestion.ListBranchesAsync(projectId, ct);
            var indexedNames = await _db.GraphSnapshots
                .Where(s => s.ProjectId == projectId && s.IsCurrent && s.Branch != null)
                .Select(s => s.Branch!.Name)
                .ToListAsync(ct);

            return Ok(branches.Select(b => new BranchResponse(b.Name, string.IsNullOrEmpty(b.HeadCommitSha) ? null : b.HeadCommitSha, b.IsDefault, indexedNames.Contains(b.Name))).ToList());
        }
        catch (Exception ex) when (ex is InvalidOperationException or DirectoryNotFoundException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{projectId:guid}/ingest")]
    [EnableRateLimiting("expensive")]
    [RequestSizeLimit(64 * 1024)]
    public async Task<ActionResult<IngestResponse>> Ingest(Guid projectId, IngestRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _ingestion.IngestAsync(projectId, request, ct));
        }
        catch (Exception ex) when (ex is InvalidOperationException or DirectoryNotFoundException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{projectId:guid}")]
    public async Task<ActionResult<ProjectResponse>> Update(Guid projectId, UpdateProjectRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _projects.UpdateAsync(projectId, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("{projectId:guid}")]
    public async Task<ActionResult> Delete(Guid projectId, CancellationToken ct)
    {
        try
        {
            await _projects.DeleteAsync(projectId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{projectId:guid}/graph")]
    public async Task<ActionResult<GraphResponse>> GetGraph(Guid projectId, [FromQuery] string? branch, CancellationToken ct)
    {
        var snapshotQuery = _db.GraphSnapshots.Where(s => s.ProjectId == projectId && s.IsCurrent);
        snapshotQuery = branch is null
            ? snapshotQuery.Where(s => s.Branch!.IsDefault || s.BranchId == null)
            : snapshotQuery.Where(s => s.Branch!.Name == branch);
        var snapshot = await snapshotQuery.FirstOrDefaultAsync(ct);
        if (snapshot is null) return Ok(new GraphResponse(null, Array.Empty<GraphNodeResponse>(), Array.Empty<GraphEdgeResponse>()));

        var componentTypes = await _db.ComponentTypes.ToDictionaryAsync(c => c.Id, c => c.Code, ct);
        var edgeTypes = await _db.GraphEdgeTypes.ToDictionaryAsync(e => e.Id, e => e.Code, ct);

        var nodes = await _db.GraphNodes.Where(n => n.GraphSnapshotId == snapshot.GraphSnapshotId).ToListAsync(ct);
        var codeFilePaths = await _db.CodeFiles.Where(f => nodes.Select(n => n.CodeFileId).Contains(f.CodeFileId))
            .ToDictionaryAsync(f => f.CodeFileId, f => f.RelativePath, ct);

        var edges = await _db.GraphEdges.Where(e => e.GraphSnapshotId == snapshot.GraphSnapshotId).ToListAsync(ct);

        var nodeResponses = nodes.Select(n => new GraphNodeResponse(
            n.GraphNodeId, n.ExternalKey, componentTypes.GetValueOrDefault(n.ComponentTypeId, "UNKNOWN"), n.DisplayName,
            n.CodeFileId is not null ? codeFilePaths.GetValueOrDefault(n.CodeFileId.Value) : null, n.IsParsed)).ToList();

        var edgeResponses = edges.Select(e => new GraphEdgeResponse(
            e.GraphEdgeId, e.SourceNodeId, e.TargetNodeId, edgeTypes.GetValueOrDefault(e.GraphEdgeTypeId, "USES"), e.Confidence, e.IsRuntimeResolved)).ToList();

        return Ok(new GraphResponse(snapshot.GraphSnapshotId, nodeResponses, edgeResponses));
    }

    [HttpGet("{projectId:guid}/graph/nodes/{nodeId:guid}")]
    public async Task<ActionResult<ProjectNodeDetailResponse>> GetNodeDetail(Guid projectId, Guid nodeId, CancellationToken ct)
    {
        var snapshot = await _db.GraphSnapshots
            .Where(s => s.ProjectId == projectId && s.IsCurrent)
            .FirstOrDefaultAsync(ct);
        if (snapshot is null)
        {
            return NotFound(new { error = "No current graph snapshot found for this project." });
        }

        var node = await _db.GraphNodes.FirstOrDefaultAsync(n => n.GraphNodeId == nodeId && n.GraphSnapshotId == snapshot.GraphSnapshotId, ct);
        if (node is null) return NotFound();

        var componentType = await _db.ComponentTypes.Where(c => c.Id == node.ComponentTypeId).Select(c => c.Code).FirstOrDefaultAsync(ct);
        var codeFile = node.CodeFileId is null ? null : await _db.CodeFiles.FirstOrDefaultAsync(f => f.CodeFileId == node.CodeFileId, ct);
        var incoming = await _db.GraphEdges
            .Where(e => e.TargetNodeId == nodeId && e.GraphSnapshotId == snapshot.GraphSnapshotId)
            .Select(e => new { e.SourceNodeId, e.GraphEdgeTypeId, e.Confidence })
            .ToListAsync(ct);
        var outgoing = await _db.GraphEdges
            .Where(e => e.SourceNodeId == nodeId && e.GraphSnapshotId == snapshot.GraphSnapshotId)
            .Select(e => new { e.TargetNodeId, e.GraphEdgeTypeId, e.Confidence })
            .ToListAsync(ct);

        var edgeTypeNames = await _db.GraphEdgeTypes.ToDictionaryAsync(e => e.Id, e => e.Code, ct);
        var neighborNames = await _db.GraphNodes.Where(n => incoming.Select(i => i.SourceNodeId).Concat(outgoing.Select(o => o.TargetNodeId)).Contains(n.GraphNodeId)).ToDictionaryAsync(n => n.GraphNodeId, n => n.DisplayName, ct);

        var contains = new List<string>();
        if (codeFile is not null) contains.Add(codeFile.RelativePath);
        if (node.DisplayName is not null && node.DisplayName.Contains('.')) contains.Add(node.DisplayName);

        var incomingDetails = incoming.Select(i => new NodeConnectionDetail(i.SourceNodeId, neighborNames.GetValueOrDefault(i.SourceNodeId, "Unknown"), edgeTypeNames.GetValueOrDefault(i.GraphEdgeTypeId, "USES"), i.Confidence, "incoming")).ToList();
        var outgoingDetails = outgoing.Select(o => new NodeConnectionDetail(o.TargetNodeId, neighborNames.GetValueOrDefault(o.TargetNodeId, "Unknown"), edgeTypeNames.GetValueOrDefault(o.GraphEdgeTypeId, "USES"), o.Confidence, "outgoing")).ToList();

        return Ok(new ProjectNodeDetailResponse(
            node.GraphNodeId,
            node.ExternalKey,
            componentType ?? "UNKNOWN",
            node.DisplayName ?? "Unknown",
            codeFile?.RelativePath,
            node.IsParsed,
            contains,
            incomingDetails,
            outgoingDetails,
            null));
    }

    [HttpGet("{projectId:guid}/graph/nodes/{nodeId:guid}/explain")]
    public async Task<ActionResult<NodeExplainResponse>> ExplainNode(Guid projectId, Guid nodeId, CancellationToken ct)
    {
        var detail = await GetNodeDetail(projectId, nodeId, ct);
        if (detail.Result is not OkObjectResult { Value: ProjectNodeDetailResponse node }) return detail.Result!;

        var fileContent = await TryReadNodeFileContentAsync(projectId, node.FilePath, ct);

        var response = await _ai.ExplainNodeAsync(
            node.DisplayName,
            node.ComponentType,
            node.FilePath,
            fileContent,
            node.Contains,
            node.Outgoing.Select(o => o.NeighborName).Distinct().ToList(),
            node.Incoming.Select(i => i.NeighborName).Distinct().ToList(),
            ct);
        return Ok(response);
    }

    /// <summary>Best-effort read of the node's source file, confined to the project's repository root.</summary>
    private async Task<string?> TryReadNodeFileContentAsync(Guid projectId, string? relativeFilePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath)) return null;

        var repository = await _db.Repositories.FirstOrDefaultAsync(r => r.ProjectId == projectId, ct);
        if (repository?.CloneUrl is null || !repository.CloneUrl.StartsWith("file:///", StringComparison.OrdinalIgnoreCase)) return null;

        var root = repository.CloneUrl["file:///".Length..].Replace('/', Path.DirectorySeparatorChar);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativeFilePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.Equals(fullRoot, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return System.IO.File.Exists(fullPath) ? await System.IO.File.ReadAllTextAsync(fullPath, ct) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    [HttpPost("{projectId:guid}/security-scans")]
    public async Task<ActionResult<ProjectSecurityScanResponse>> RunProjectSecurityScan(Guid projectId, CancellationToken ct)
    {
        var project = await _projects.GetAsync(projectId, ct);
        if (project is null) return NotFound();

        var repository = await _db.Repositories.FirstOrDefaultAsync(r => r.ProjectId == projectId, ct);
        if (repository?.CloneUrl is null || !repository.CloneUrl.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "This project is not backed by a local file repository for a project-wide scan." });
        }

        var root = repository.CloneUrl["file:///".Length..].Replace('/', Path.DirectorySeparatorChar);

        // Always run a fresh scan — previously this reused whatever scan already existed for the
        // project, so "Run Security Scan" silently did nothing after the first run.
        var result = await _projects.CreateProjectSecurityScanAsync(projectId, root, ct);

        return Ok(new ProjectSecurityScanResponse(result.SecurityScanId, projectId, result.FindingCount, result.StatusCode, result.StartedAtUtc ?? DateTime.UtcNow, result.CompletedAtUtc));
    }

    [HttpGet("overview")]
    public async Task<ActionResult<OverviewResponse>> Overview(CancellationToken ct)
    {
        var org = await ResolveOrganizationAsync(ct);
        return Ok(await _projects.GetOverviewAsync(org, ct));
    }

    private async Task<Guid> ResolveOrganizationAsync(CancellationToken ct)
    {
        var user = await _currentUser.GetOrProvisionUserAsync(User, ct);
        var org = await _currentUser.GetOrProvisionOrganizationAsync(user, ct);
        return org.OrganizationId;
    }
}
