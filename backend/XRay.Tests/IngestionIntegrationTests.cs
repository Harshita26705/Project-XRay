using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using XRay.Api.Contracts;
using XRay.Api.Services;
using XRay.Api.Services.Repositories;
using XRay.Domain.Graph;
using XRay.Domain.Projects;
using XRay.Infrastructure.Persistence;
using XRay.Parsers;

namespace XRay.Tests;

public class IngestionIntegrationTests
{
    [Fact]
    public async Task IngestAsync_LocalFixture_MaterializesParsedGraph()
    {
        var root = Path.Combine(Path.GetTempPath(), $"xray-fixture-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Orders.cs"), "namespace Demo; public class OrdersService { }");
        await File.WriteAllTextAsync(Path.Combine(root, "schema.sql"), "CREATE TABLE Orders (Id int);");

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var db = new AppDbContext(options);
            db.ComponentTypes.AddRange(new[] { ComponentTypeCodes.Service, ComponentTypeCodes.DatabaseTable, ComponentTypeCodes.Unknown }
                .Select((code, index) => new XRay.Domain.Reference.ComponentType { Id = (byte)(index + 1), Code = code, DisplayName = code }));
            db.GraphEdgeTypes.Add(new XRay.Domain.Reference.GraphEdgeType { Id = 1, Code = GraphEdgeTypeCodes.Uses, DisplayName = "Uses", DefaultCost = 1m });
            var projectId = Guid.NewGuid();
            db.Repositories.Add(new Repository
            {
                RepositoryId = Guid.NewGuid(), ProjectId = projectId, Name = "fixture", CloneUrl = $"file:///{root.Replace('\\', '/')}",
                DefaultBranchName = "main", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var factory = new RepositoryProviderFactory(new[] { new LocalRepositoryProvider() });
            var configuration = new ConfigurationBuilder().Build();
            var service = new IngestionService(db, factory, configuration, NullLogger<IngestionService>.Instance);

            var response = await service.IngestAsync(projectId, new IngestRequest(root, "main"));

            Assert.Equal(2, response.FilesDiscovered);
            Assert.Equal(2, response.FilesParsed);
            Assert.Contains(await db.GraphNodes.ToListAsync(), node => node.ExternalKey == "CLASS:Demo.OrdersService");
            Assert.Contains(await db.GraphNodes.ToListAsync(), node => node.ExternalKey == "TABLE:Orders");
            Assert.Single(await db.GraphSnapshots.Where(snapshot => snapshot.ProjectId == projectId && snapshot.IsCurrent).ToListAsync());

            var secondResponse = await service.IngestAsync(projectId, new IngestRequest(root, "main"));

            Assert.Equal(0, secondResponse.FilesParsed);
            Assert.Equal(0, secondResponse.FilesFailed);
            var currentSnapshotId = await db.GraphSnapshots
                .Where(snapshot => snapshot.ProjectId == projectId && snapshot.IsCurrent)
                .Select(snapshot => snapshot.GraphSnapshotId)
                .SingleAsync();
            Assert.Equal(2, await db.GraphNodes.CountAsync(node => node.GraphSnapshotId == currentSnapshotId));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}