using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Identity;
using XRay.Domain.Projects;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

public class ProjectService
{
    private readonly AppDbContext _db;

    public ProjectService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProjectResponse>> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        var projects = await _db.Projects
            .Where(p => p.OrganizationId == organizationId && p.IsActive)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);

        var result = new List<ProjectResponse>();
        foreach (var project in projects)
        {
            result.Add(await ToResponseAsync(project, ct));
        }
        return result;
    }

    public async Task<ProjectResponse?> GetAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId, ct);
        return project is null ? null : await ToResponseAsync(project, ct);
    }

    public async Task<ProjectResponse> CreateAsync(Guid organizationId, Guid createdByUserId, CreateProjectRequest request, CancellationToken ct = default)
    {
        var project = new Project
        {
            ProjectId = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name,
            Description = request.Description,
            ExternalProjectUrl = request.ExternalProjectUrl,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        _db.Projects.Add(project);

        var repository = new Repository
        {
            RepositoryId = Guid.NewGuid(),
            ProjectId = project.ProjectId,
            Name = request.Name,
            // Local filesystem path is stored as a file:// clone URL — this demo ingests from disk
            // rather than a live Azure DevOps clone, so there is no dedicated "local path" column in schema.md.
            CloneUrl = request.LocalRepositoryPath is null ? null : $"file:///{request.LocalRepositoryPath.Replace('\\', '/')}",
            WebUrl = request.ExternalProjectUrl,
            DefaultBranchName = "main",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        _db.Repositories.Add(repository);

        _db.ProjectEnvironments.Add(new ProjectEnvironment
        {
            EnvironmentId = Guid.NewGuid(),
            ProjectId = project.ProjectId,
            Name = "Development",
            CreatedAtUtc = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        return await ToResponseAsync(project, ct);
    }

    public async Task<OverviewResponse> GetOverviewAsync(Guid organizationId, CancellationToken ct = default)
    {
        var projectIds = await _db.Projects.Where(p => p.OrganizationId == organizationId).Select(p => p.ProjectId).ToListAsync(ct);

        var analyses = await _db.Analyses
            .Where(a => projectIds.Contains(a.ProjectId))
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(10)
            .ToListAsync(ct);

        var riskStates = await _db.RiskStates.ToDictionaryAsync(r => r.Id, r => r.Code, ct);
        var statuses = await _db.AnalysisStatuses.ToDictionaryAsync(s => s.Id, s => s.Code, ct);
        var changeTypes = await _db.ChangeTypes.ToDictionaryAsync(c => c.Id, c => c.Code, ct);

        var recent = new List<RecentAnalysisResponse>();
        foreach (var a in analyses)
        {
            var change = a.ChangeId is null ? null : await _db.Changes.FirstOrDefaultAsync(c => c.ChangeId == a.ChangeId, ct);
            var affected = await _db.AnalysisNodeResults.CountAsync(n => n.AnalysisId == a.AnalysisId && n.RiskStateId != 3, ct);
            recent.Add(new RecentAnalysisResponse(
                a.AnalysisId,
                change?.Title ?? "(untitled change)",
                change is null ? "MANUAL" : changeTypes.GetValueOrDefault(change.ChangeTypeId, "MANUAL"),
                a.OverallRiskStateId is null ? null : riskStates.GetValueOrDefault(a.OverallRiskStateId.Value),
                affected,
                a.CreatedAtUtc,
                statuses.GetValueOrDefault(a.AnalysisStatusId, "QUEUED")));
        }

        var totalAnalyses = await _db.Analyses.CountAsync(a => projectIds.Contains(a.ProjectId), ct);
        var criticalChanges = await _db.Analyses.CountAsync(a => projectIds.Contains(a.ProjectId) && a.OverallRiskStateId == 1, ct);
        var componentsAnalyzed = await _db.GraphNodes.CountAsync(n => _db.GraphSnapshots.Any(s => s.GraphSnapshotId == n.GraphSnapshotId && projectIds.Contains(s.ProjectId)), ct);
        var securityFindings = await _db.SecurityFindings.CountAsync(f => _db.SecurityScans.Any(s => s.SecurityScanId == f.SecurityScanId && _db.Analyses.Any(a => a.AnalysisId == s.AnalysisId && projectIds.Contains(a.ProjectId))), ct);

        return new OverviewResponse(totalAnalyses, criticalChanges, componentsAnalyzed, securityFindings, recent);
    }

    private async Task<ProjectResponse> ToResponseAsync(Project project, CancellationToken ct)
    {
        var repository = await _db.Repositories.FirstOrDefaultAsync(r => r.ProjectId == project.ProjectId, ct);
        var snapshot = await _db.GraphSnapshots.Where(s => s.ProjectId == project.ProjectId && s.IsCurrent).FirstOrDefaultAsync(ct);

        var nodeCount = snapshot is null ? 0 : await _db.GraphNodes.CountAsync(n => n.GraphSnapshotId == snapshot.GraphSnapshotId, ct);
        var edgeCount = snapshot is null ? 0 : await _db.GraphEdges.CountAsync(e => e.GraphSnapshotId == snapshot.GraphSnapshotId, ct);
        var securityCount = await _db.SecurityFindings.CountAsync(f =>
            _db.SecurityScans.Any(s => s.SecurityScanId == f.SecurityScanId &&
                                        _db.Analyses.Any(a => a.AnalysisId == s.AnalysisId && a.ProjectId == project.ProjectId)) &&
            f.StatusCode == "OPEN", ct);

        var tags = new List<string>();
        if (repository?.CloneUrl is not null) tags.Add(".NET");
        if (nodeCount > 0) tags.Add("React");
        tags.Add("SQL Server");
        tags.Add("Azure DevOps");

        return new ProjectResponse(
            project.ProjectId, project.Name, project.Description, project.IsActive,
            repository?.CloneUrl ?? repository?.WebUrl,
            repository?.LastIndexedAtUtc is null ? "NOT_CONNECTED" : "CONNECTED",
            nodeCount, edgeCount, securityCount, repository?.LastIndexedAtUtc, tags);
    }
}
