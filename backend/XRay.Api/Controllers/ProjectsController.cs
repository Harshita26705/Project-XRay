using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public ProjectsController(ProjectService projects, IngestionService ingestion, ICurrentUserService currentUser, AppDbContext db)
    {
        _projects = projects;
        _ingestion = ingestion;
        _currentUser = currentUser;
        _db = db;
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

    [HttpGet("{projectId:guid}/graph")]
    public async Task<ActionResult<GraphResponse>> GetGraph(Guid projectId, [FromQuery] string? branch, CancellationToken ct)
    {
        var snapshotQuery = _db.GraphSnapshots.Where(s => s.ProjectId == projectId && s.IsCurrent);
        snapshotQuery = branch is null
            ? snapshotQuery.Where(s => s.Branch!.IsDefault || s.BranchId == null)
            : snapshotQuery.Where(s => s.Branch!.Name == branch);
        var snapshot = await snapshotQuery.FirstOrDefaultAsync(ct)
                       ?? await _db.GraphSnapshots.Where(s => s.ProjectId == projectId && s.IsCurrent).FirstOrDefaultAsync(ct);
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
