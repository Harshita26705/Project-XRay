using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Changes;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

public class ChangeService
{
    private readonly AppDbContext _db;

    public ChangeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChangeResponse> CreateAsync(Guid organizationId, Guid projectId, Guid authorUserId, CreateChangeRequest request, CancellationToken ct = default)
    {
        var changeTypeId = await _db.ChangeTypes.Where(c => c.Code == request.ChangeType).Select(c => c.Id).FirstOrDefaultAsync(ct);
        if (changeTypeId == 0) throw new InvalidOperationException($"Unknown change type '{request.ChangeType}'.");

        var change = new Change
        {
            ChangeId = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            ChangeTypeId = changeTypeId,
            Title = request.Title,
            Description = request.Description,
            ExternalChangeId = request.ExternalChangeId,
            AuthorUserId = authorUserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        _db.Changes.Add(change);

        foreach (var path in request.ChangedFilePaths)
        {
            var codeFile = await _db.CodeFiles.FirstOrDefaultAsync(f =>
                _db.Repositories.Any(r => r.ProjectId == projectId && r.RepositoryId == f.RepositoryId) && f.RelativePath == path, ct);

            _db.ChangeFiles.Add(new ChangeFile
            {
                ChangeFileId = Guid.NewGuid(),
                ChangeId = change.ChangeId,
                CodeFileId = codeFile?.CodeFileId,
                RelativePath = path,
                ChangeKindCode = "MODIFIED",
            });
        }

        if (request.ChangeType == "WORK_ITEM")
        {
            _db.WorkItems.Add(new WorkItem
            {
                WorkItemId = Guid.NewGuid(),
                ChangeId = change.ChangeId,
                ExternalWorkItemId = request.ExternalChangeId ?? Guid.NewGuid().ToString("N")[..6],
                Priority = "Medium",
                State = "Active",
            });
        }
        else if (request.ChangeType == "PULL_REQUEST")
        {
            var repository = await _db.Repositories.FirstOrDefaultAsync(r => r.ProjectId == projectId, ct);
            if (repository is not null)
            {
                _db.PullRequests.Add(new PullRequest
                {
                    PullRequestId = Guid.NewGuid(),
                    ChangeId = change.ChangeId,
                    RepositoryId = repository.RepositoryId,
                    ProviderPullRequestId = request.ExternalChangeId ?? Guid.NewGuid().ToString("N")[..6],
                    FilesChangedCount = request.ChangedFilePaths.Count,
                    ProviderStatusCode = "OPEN",
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return await ToResponseAsync(change, ct);
    }

    public async Task<IReadOnlyList<ChangeResponse>> ListAsync(Guid projectId, CancellationToken ct = default)
    {
        var changes = await _db.Changes.Where(c => c.ProjectId == projectId).OrderByDescending(c => c.CreatedAtUtc).ToListAsync(ct);
        var result = new List<ChangeResponse>();
        foreach (var c in changes) result.Add(await ToResponseAsync(c, ct));
        return result;
    }

    public async Task<ChangeResponse?> GetAsync(Guid changeId, CancellationToken ct = default)
    {
        var change = await _db.Changes.FirstOrDefaultAsync(c => c.ChangeId == changeId, ct);
        return change is null ? null : await ToResponseAsync(change, ct);
    }

    private async Task<ChangeResponse> ToResponseAsync(Change change, CancellationToken ct)
    {
        var changeType = await _db.ChangeTypes.Where(t => t.Id == change.ChangeTypeId).Select(t => t.Code).FirstAsync(ct);
        var files = await _db.ChangeFiles.Where(f => f.ChangeId == change.ChangeId).Select(f => f.RelativePath).ToListAsync(ct);
        var author = change.AuthorUserId is null ? null
            : await _db.UserAccounts.Where(u => u.UserId == change.AuthorUserId).Select(u => u.DisplayName).FirstOrDefaultAsync(ct);

        var latestAnalysis = await _db.Analyses.Where(a => a.ChangeId == change.ChangeId).OrderByDescending(a => a.CreatedAtUtc).FirstOrDefaultAsync(ct);
        string? riskState = null;
        var affected = 0;
        var status = "PENDING";
        if (latestAnalysis is not null)
        {
            if (latestAnalysis.OverallRiskStateId is not null)
                riskState = await _db.RiskStates.Where(r => r.Id == latestAnalysis.OverallRiskStateId).Select(r => r.Code).FirstOrDefaultAsync(ct);
            affected = await _db.AnalysisNodeResults.CountAsync(n => n.AnalysisId == latestAnalysis.AnalysisId && n.RiskStateId != 3, ct);
            status = latestAnalysis.CompletedAtUtc is not null ? "ANALYZED" : "PENDING";
        }

        return new ChangeResponse(change.ChangeId, changeType, change.Title, change.Description, author, change.CreatedAtUtc, riskState, affected, status, files);
    }
}
