using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using XRay.Api.Contracts;
using XRay.Api.Services;
using XRay.Api.Services.BackgroundJobs;
using XRay.Domain.Changes;
using XRay.Domain.Graph;
using XRay.Domain.Projects;
using XRay.Domain.Reference;
using XRay.Infrastructure.Persistence;

namespace XRay.Tests;

public class Phase4BackgroundJobTests
{
    [Fact]
    public async Task BackgroundTaskQueue_DequeuesInFifoOrder()
    {
        var queue = new BackgroundTaskQueue();
        var callOrder = new List<int>();

        await queue.EnqueueAsync((_, _) => { callOrder.Add(1); return Task.CompletedTask; });
        await queue.EnqueueAsync((_, _) => { callOrder.Add(2); return Task.CompletedTask; });

        var first = await queue.DequeueAsync(CancellationToken.None);
        var second = await queue.DequeueAsync(CancellationToken.None);
        await first(null!, CancellationToken.None);
        await second(null!, CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, callOrder);
    }

    private static async Task<(AppDbContext Db, Guid OrgId, Guid ChangeId, Guid SnapshotId)> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.RiskStates.AddRange(
            new RiskState { Id = 1, Code = "CRITICAL", DisplayName = "Critical", SortOrder = 1 },
            new RiskState { Id = 2, Code = "RISKY", DisplayName = "Risky", SortOrder = 2 },
            new RiskState { Id = 3, Code = "SAFE", DisplayName = "Safe", SortOrder = 3 },
            new RiskState { Id = 4, Code = "UNKNOWN", DisplayName = "Unknown", SortOrder = 4 });
        db.AnalysisStatuses.AddRange(
            new AnalysisStatus { Id = 1, Code = "QUEUED", DisplayName = "Queued" },
            new AnalysisStatus { Id = 2, Code = "RUNNING", DisplayName = "Running" },
            new AnalysisStatus { Id = 3, Code = "COMPLETED", DisplayName = "Completed" },
            new AnalysisStatus { Id = 4, Code = "FAILED", DisplayName = "Failed" });

        var orgId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        byte changeTypeId = 1;

        db.Projects.Add(new Project { ProjectId = projectId, OrganizationId = orgId, Name = "Demo", CreatedByUserId = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        db.ChangeTypes.Add(new ChangeType { Id = changeTypeId, Code = "MANUAL", DisplayName = "Manual" });
        db.Changes.Add(new Change { ChangeId = changeId, ProjectId = projectId, ChangeTypeId = changeTypeId, Title = "Demo change", CreatedAtUtc = DateTime.UtcNow });
        db.GraphSnapshots.Add(new GraphSnapshot { GraphSnapshotId = snapshotId, ProjectId = projectId, IngestionRunId = Guid.NewGuid(), IsCurrent = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        return (db, orgId, changeId, snapshotId);
    }

    [Fact]
    public async Task AnalysisService_CreateAndRunAsync_QueuesWorkInsteadOfRunningInline()
    {
        var (db, orgId, changeId, _) = await SeedAsync();
        await using var _db = db;

        var queue = new CapturingBackgroundTaskQueue();
        var service = new AnalysisService(db, new SecurityScanService(db), queue, NullLogger<AnalysisService>.Instance);

        var request = new CreateAnalysisRequest(changeId, new AnalysisScopeRequest(RunStaticSecurityAnalysis: false), null);
        var response = await service.CreateAndRunAsync(orgId, Guid.NewGuid(), request, CancellationToken.None);

        Assert.Equal("QUEUED", response.Status);
        Assert.Single(queue.Enqueued);
        var job = await db.Jobs.SingleAsync(j => j.CorrelationId == response.AnalysisId);
        Assert.Equal("ANALYSIS", job.JobTypeCode);
        Assert.Equal("QUEUED", job.StatusCode);
    }

    [Fact]
    public async Task AnalysisService_RunQueuedAnalysisAsync_TransitionsAnalysisAndJobToCompleted()
    {
        var (db, orgId, changeId, snapshotId) = await SeedAsync();
        await using var _db = db;

        var queue = new CapturingBackgroundTaskQueue();
        var service = new AnalysisService(db, new SecurityScanService(db), queue, NullLogger<AnalysisService>.Instance);

        var request = new CreateAnalysisRequest(changeId, new AnalysisScopeRequest(RunStaticSecurityAnalysis: false), null);
        var response = await service.CreateAndRunAsync(orgId, Guid.NewGuid(), request, CancellationToken.None);
        var job = await db.Jobs.SingleAsync(j => j.CorrelationId == response.AnalysisId);

        await service.RunQueuedAnalysisAsync(response.AnalysisId, job.JobId, snapshotId, changeId, runStaticSecurityAnalysis: false, CancellationToken.None);

        var updatedAnalysis = await db.Analyses.FirstAsync(a => a.AnalysisId == response.AnalysisId);
        var statusCode = await db.AnalysisStatuses.Where(s => s.Id == updatedAnalysis.AnalysisStatusId).Select(s => s.Code).FirstAsync();
        Assert.Equal("COMPLETED", statusCode);

        var updatedJob = await db.Jobs.SingleAsync(j => j.JobId == job.JobId);
        Assert.Equal("COMPLETED", updatedJob.StatusCode);
        Assert.NotNull(updatedJob.CompletedAtUtc);
    }

    private sealed class CapturingBackgroundTaskQueue : IBackgroundTaskQueue
    {
        public List<Func<IServiceProvider, CancellationToken, Task>> Enqueued { get; } = new();

        public ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem)
        {
            Enqueued.Add(workItem);
            return ValueTask.CompletedTask;
        }

        public ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken ct) =>
            throw new NotSupportedException("Test double never dequeues; work items are captured for inspection.");
    }
}
