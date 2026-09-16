using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using XRay.Api.Contracts;
using XRay.Api.Services;
using XRay.Domain.Ai;
using XRay.Domain.Analysis;
using XRay.Domain.Changes;
using XRay.Domain.Graph;
using XRay.Domain.Identity;
using XRay.Domain.Ingestion;
using XRay.Domain.Projects;
using XRay.Domain.Reference;
using XRay.Domain.Security;
using XRay.Infrastructure.Persistence;

namespace XRay.Tests;

public class Phase1BackendSurfaceTests
{
    [Fact]
    public async Task ProjectService_UpdateAsync_UpdatesProjectMetadata()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var project = new Project
        {
            ProjectId = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "Old Name",
            Description = "Old description",
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var service = new ProjectService(db);
        var updated = await service.UpdateAsync(project.ProjectId, new UpdateProjectRequest("New Name", "New description"), CancellationToken.None);

        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New description", updated.Description);
    }

    [Fact]
    public async Task SecurityScanService_RunProjectScanAsync_ScansCurrentSnapshotFiles()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        var orgId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();
        var scannerId = Guid.NewGuid();

        db.SecurityScanners.Add(new SecurityScanner { SecurityScannerId = scannerId, OrganizationId = orgId, Name = "X-Ray Structural SAST", Code = "XRAY_REGEX_SAST" });
        db.Projects.Add(new Project { ProjectId = projectId, OrganizationId = orgId, Name = "Demo", CreatedByUserId = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        db.Repositories.Add(new Repository { RepositoryId = repositoryId, ProjectId = projectId, Name = "Demo", CloneUrl = "file:///tmp/demo", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        db.Analyses.Add(new Analysis { AnalysisId = analysisId, OrganizationId = orgId, ProjectId = projectId, AnalysisStatusId = 1, RequestedByUserId = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow });

        var snapshotId = Guid.NewGuid();
        db.GraphSnapshots.Add(new GraphSnapshot { GraphSnapshotId = snapshotId, ProjectId = projectId, IngestionRunId = Guid.NewGuid(), IsCurrent = true, CreatedAtUtc = DateTime.UtcNow });
        var nodeId = Guid.NewGuid();
        db.GraphNodes.Add(new GraphNode { GraphNodeId = nodeId, GraphSnapshotId = snapshotId, ComponentTypeId = 1, ExternalKey = "CLASS:Demo.SecretService", DisplayName = "Demo.SecretService", IsParsed = true, CreatedAtUtc = DateTime.UtcNow });
        db.CodeFiles.Add(new CodeFile { CodeFileId = Guid.NewGuid(), RepositoryId = repositoryId, RelativePath = "SecretService.cs", LanguageCode = "CSharp", FileExtension = ".cs", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });

        await db.SaveChangesAsync();

        var tempRoot = Path.Combine(Path.GetTempPath(), $"xray-phase1-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var filePath = Path.Combine(tempRoot, "SecretService.cs");
        await File.WriteAllTextAsync(filePath, "class SecretService { string password = \"supersecret\"; }");

        try
        {
            var service = new SecurityScanService(db);
            var result = await service.RunProjectScanAsync(projectId, tempRoot, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.FindingCount > 0);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task AnalysisService_GetImpactGraphAsync_ReturnsPathEdgesAcrossBestPaths()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        var projectId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        byte changeTypeId = 1;

        db.RiskStates.AddRange(
            new RiskState { Id = 1, Code = "CRITICAL", DisplayName = "Critical", Description = "Critical", SortOrder = 1 },
            new RiskState { Id = 2, Code = "RISKY", DisplayName = "Risky", Description = "Risky", SortOrder = 2 },
            new RiskState { Id = 3, Code = "SAFE", DisplayName = "Safe", Description = "Safe", SortOrder = 3 },
            new RiskState { Id = 4, Code = "UNKNOWN", DisplayName = "Unknown", Description = "Unknown", SortOrder = 4 });
        db.ComponentTypes.Add(new ComponentType { Id = 1, Code = "SERVICE", DisplayName = "Service" });
        db.GraphEdgeTypes.Add(new GraphEdgeType { Id = 1, Code = "CALLS", DisplayName = "Calls", DefaultCost = 1m });
        db.AnalysisStatuses.Add(new AnalysisStatus { Id = 1, Code = "COMPLETED", DisplayName = "Completed" });

        db.Projects.Add(new Project { ProjectId = projectId, OrganizationId = Guid.NewGuid(), Name = "Demo", CreatedByUserId = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        db.ChangeTypes.Add(new ChangeType { Id = changeTypeId, Code = "MANUAL", DisplayName = "Manual" });
        db.Changes.Add(new Change { ChangeId = changeId, ProjectId = projectId, ChangeTypeId = changeTypeId, Title = "Demo change", CreatedAtUtc = DateTime.UtcNow });
        db.GraphSnapshots.Add(new GraphSnapshot { GraphSnapshotId = snapshotId, ProjectId = projectId, IngestionRunId = Guid.NewGuid(), IsCurrent = true, CreatedAtUtc = DateTime.UtcNow });
        db.GraphNodes.AddRange(
            new GraphNode { GraphNodeId = sourceId, GraphSnapshotId = snapshotId, ComponentTypeId = 1, ExternalKey = "CLASS:Demo.Source", DisplayName = "Demo.Source", IsParsed = true, CreatedAtUtc = DateTime.UtcNow },
            new GraphNode { GraphNodeId = targetId, GraphSnapshotId = snapshotId, ComponentTypeId = 1, ExternalKey = "CLASS:Demo.Target", DisplayName = "Demo.Target", IsParsed = true, CreatedAtUtc = DateTime.UtcNow });
        db.GraphEdges.Add(new GraphEdge { GraphEdgeId = Guid.NewGuid(), GraphSnapshotId = snapshotId, GraphEdgeTypeId = 1, SourceNodeId = sourceId, TargetNodeId = targetId, Confidence = 0.98m, CreatedAtUtc = DateTime.UtcNow });
        db.Analyses.Add(new Analysis { AnalysisId = analysisId, OrganizationId = Guid.NewGuid(), ProjectId = projectId, ChangeId = changeId, GraphSnapshotId = snapshotId, AnalysisStatusId = 1, RequestedByUserId = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow });
        db.AnalysisNodeResults.Add(new AnalysisNodeResult { AnalysisNodeResultId = Guid.NewGuid(), AnalysisId = analysisId, GraphNodeId = targetId, RiskStateId = 2, Distance = 1, MinPathConfidence = 0.98m, RuleCode = "R5", IsDirectlyChanged = false, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new AnalysisService(db, new SecurityScanService(db), NullLogger<AnalysisService>.Instance);
        var response = await service.GetImpactGraphAsync(analysisId, CancellationToken.None);

        Assert.NotEmpty(response);
        Assert.Contains(response, edge => edge.SourceNodeId == sourceId && edge.TargetNodeId == targetId);
    }

    [Fact]
    public async Task AnalysisService_GetAsync_ExposesImpactEdgesAndRuleRecommendations()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        var projectId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        byte changeTypeId = 1;

        db.RiskStates.AddRange(
            new RiskState { Id = 1, Code = "CRITICAL", DisplayName = "Critical", Description = "Critical", SortOrder = 1 },
            new RiskState { Id = 2, Code = "RISKY", DisplayName = "Risky", Description = "Risky", SortOrder = 2 },
            new RiskState { Id = 3, Code = "SAFE", DisplayName = "Safe", Description = "Safe", SortOrder = 3 },
            new RiskState { Id = 4, Code = "UNKNOWN", DisplayName = "Unknown", Description = "Unknown", SortOrder = 4 });
        db.ComponentTypes.Add(new ComponentType { Id = 1, Code = "SERVICE", DisplayName = "Service" });
        db.GraphEdgeTypes.Add(new GraphEdgeType { Id = 1, Code = "CALLS", DisplayName = "Calls", DefaultCost = 1m });
        db.AnalysisStatuses.Add(new AnalysisStatus { Id = 1, Code = "COMPLETED", DisplayName = "Completed" });

        db.Projects.Add(new Project { ProjectId = projectId, OrganizationId = Guid.NewGuid(), Name = "Demo", CreatedByUserId = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        db.ChangeTypes.Add(new ChangeType { Id = changeTypeId, Code = "MANUAL", DisplayName = "Manual" });
        db.Changes.Add(new Change { ChangeId = changeId, ProjectId = projectId, ChangeTypeId = changeTypeId, Title = "Demo change", CreatedAtUtc = DateTime.UtcNow });
        db.GraphSnapshots.Add(new GraphSnapshot { GraphSnapshotId = snapshotId, ProjectId = projectId, IngestionRunId = Guid.NewGuid(), IsCurrent = true, CreatedAtUtc = DateTime.UtcNow });
        db.GraphNodes.AddRange(
            new GraphNode { GraphNodeId = sourceId, GraphSnapshotId = snapshotId, ComponentTypeId = 1, ExternalKey = "CLASS:Demo.Source", DisplayName = "Demo.Source", IsParsed = true, CreatedAtUtc = DateTime.UtcNow },
            new GraphNode { GraphNodeId = targetId, GraphSnapshotId = snapshotId, ComponentTypeId = 1, ExternalKey = "CLASS:Demo.Target", DisplayName = "Demo.Target", IsParsed = true, CreatedAtUtc = DateTime.UtcNow });
        db.GraphEdges.Add(new GraphEdge { GraphEdgeId = Guid.NewGuid(), GraphSnapshotId = snapshotId, GraphEdgeTypeId = 1, SourceNodeId = sourceId, TargetNodeId = targetId, Confidence = 0.98m, CreatedAtUtc = DateTime.UtcNow });
        db.Analyses.Add(new Analysis { AnalysisId = analysisId, OrganizationId = Guid.NewGuid(), ProjectId = projectId, ChangeId = changeId, GraphSnapshotId = snapshotId, AnalysisStatusId = 1, RequestedByUserId = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow });
        db.AnalysisNodeResults.Add(new AnalysisNodeResult { AnalysisNodeResultId = Guid.NewGuid(), AnalysisId = analysisId, GraphNodeId = targetId, RiskStateId = 2, Distance = 1, MinPathConfidence = 0.98m, RuleCode = "R5", IsDirectlyChanged = false, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new AnalysisService(db, new SecurityScanService(db), NullLogger<AnalysisService>.Instance);
        var response = await service.GetAsync(analysisId, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Contains(response.Edges, edge => edge.SourceNodeId == sourceId && edge.TargetNodeId == targetId);
        Assert.Contains(response.Nodes, node => node.RuleCode == "R5" && !string.IsNullOrWhiteSpace(node.Recommendation));
    }
}
