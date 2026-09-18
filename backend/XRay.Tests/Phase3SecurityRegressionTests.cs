using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using XRay.Api.Auth;
using XRay.Api.Contracts;
using XRay.Api.Services;
using XRay.Api.Services.Repositories;
using XRay.Domain.Analysis;
using XRay.Domain.Integrations;
using XRay.Domain.Projects;
using XRay.Domain.Reference;
using XRay.Infrastructure.Persistence;

namespace XRay.Tests;

public class Phase3SecurityRegressionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DevAuthBypassGuard_AllowsSafeCombinations(bool isDevelopment, bool useDevAuthBypass)
    {
        var exception = Record.Exception(() => DevAuthBypassGuard.EnsureNotEnabledOutsideDevelopment(isDevelopment, useDevAuthBypass));
        Assert.Null(exception);
    }

    [Fact]
    public void DevAuthBypassGuard_ThrowsWhenEnabledOutsideDevelopment()
    {
        Assert.Throws<InvalidOperationException>(() => DevAuthBypassGuard.EnsureNotEnabledOutsideDevelopment(isDevelopmentEnvironment: false, useDevAuthBypassConfigured: true));
    }

    [Fact]
    public async Task IntegrationService_TestConnectionAsync_UnreachableProviderReportsError_NotAutoSuccess()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);

        var orgId = Guid.NewGuid();
        var providerId = (byte)1;
        db.IntegrationProviders.Add(new IntegrationProvider { Id = providerId, Code = "AZURE_AI_SEARCH", DisplayName = "Azure AI Search" });
        var connectionId = Guid.NewGuid();
        db.IntegrationConnections.Add(new IntegrationConnection
        {
            IntegrationConnectionId = connectionId,
            OrganizationId = orgId,
            IntegrationProviderId = providerId,
            DisplayName = "Test",
            ExternalBaseUrl = null, // unreachable: no endpoint configured
            StatusCode = "CONFIGURED",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = new IntegrationService(db, new ThrowingHttpClientFactory());
        var result = await service.TestConnectionAsync(connectionId, CancellationToken.None);

        // Previously this provider branch returned a hardcoded `true` regardless of reachability.
        Assert.Equal("ERROR", result.Status);
    }

    [Fact]
    public async Task AiExplanationService_ExplainAsync_DegradesAndNeverCallsHttpWhenApiKeyIsPlaceholder()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);

        var analysisId = Guid.NewGuid();
        db.Analyses.Add(new Analysis
        {
            AnalysisId = analysisId,
            OrganizationId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            AnalysisStatusId = 1,
            RequestedByUserId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Gemini:ApiKey"] = "REPLACE-WITH-YOUR-GEMINI-API-KEY" })
            .Build();

        var service = new AiExplanationService(db, configuration, new ThrowingHttpClientFactory());
        var response = await service.ExplainAsync(analysisId, new ExplainRequest(null), CancellationToken.None);

        Assert.True(response.Degraded);
    }

    /// <summary>Proves a code path never attempts an outbound HTTP call: any use throws immediately.</summary>
    private sealed class ThrowingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            throw new InvalidOperationException("HTTP client should not have been created for this code path.");
    }

    [Fact]
    public async Task IngestionService_IngestAsync_RejectsLocalRepositoryPathOutsideAllowedRoots()
    {
        var allowedRoot = Path.Combine(Path.GetTempPath(), $"xray-allowed-{Guid.NewGuid():N}");
        var outsidePath = Path.Combine(Path.GetTempPath(), $"xray-outside-{Guid.NewGuid():N}");
        Directory.CreateDirectory(allowedRoot);
        Directory.CreateDirectory(outsidePath);
        await File.WriteAllTextAsync(Path.Combine(outsidePath, "Secret.cs"), "namespace Demo; public class Secret { }");

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var db = new AppDbContext(options);
            var projectId = Guid.NewGuid();
            db.Repositories.Add(new Repository
            {
                RepositoryId = Guid.NewGuid(), ProjectId = projectId, Name = "fixture", CloneUrl = $"file:///{allowedRoot.Replace('\\', '/')}",
                DefaultBranchName = "main", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var factory = new RepositoryProviderFactory(new[] { new LocalRepositoryProvider() });
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Ingestion:AllowedLocalRepositoryRoots:0"] = allowedRoot })
                .Build();
            var service = new IngestionService(db, factory, configuration, NullLogger<IngestionService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.IngestAsync(projectId, new IngestRequest(outsidePath, "main")));
        }
        finally
        {
            Directory.Delete(allowedRoot, recursive: true);
            Directory.Delete(outsidePath, recursive: true);
        }
    }

    [Fact]
    public async Task IngestionService_IngestAsync_AllowsLocalRepositoryPathInsideAllowedRoot()
    {
        var allowedRoot = Path.Combine(Path.GetTempPath(), $"xray-allowed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(allowedRoot);
        await File.WriteAllTextAsync(Path.Combine(allowedRoot, "Orders.cs"), "namespace Demo; public class OrdersService { }");

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var db = new AppDbContext(options);
            db.ComponentTypes.AddRange(new[] { XRay.Parsers.ComponentTypeCodes.Service, XRay.Parsers.ComponentTypeCodes.Unknown }
                .Select((code, index) => new ComponentType { Id = (byte)(index + 1), Code = code, DisplayName = code }));
            db.GraphEdgeTypes.Add(new GraphEdgeType { Id = 1, Code = XRay.Parsers.GraphEdgeTypeCodes.Uses, DisplayName = "Uses", DefaultCost = 1m });
            var projectId = Guid.NewGuid();
            db.Repositories.Add(new Repository
            {
                RepositoryId = Guid.NewGuid(), ProjectId = projectId, Name = "fixture", CloneUrl = $"file:///{allowedRoot.Replace('\\', '/')}",
                DefaultBranchName = "main", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var factory = new RepositoryProviderFactory(new[] { new LocalRepositoryProvider() });
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Ingestion:AllowedLocalRepositoryRoots:0"] = allowedRoot })
                .Build();
            var service = new IngestionService(db, factory, configuration, NullLogger<IngestionService>.Instance);

            var response = await service.IngestAsync(projectId, new IngestRequest(allowedRoot, "main"));

            Assert.Equal(1, response.FilesParsed);
        }
        finally
        {
            Directory.Delete(allowedRoot, recursive: true);
        }
    }

    [Fact]
    public async Task LocalRepositoryProvider_GetFileContentAsync_RejectsPathEscapingRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"xray-root-{Guid.NewGuid():N}");
        var outside = Path.Combine(Path.GetTempPath(), $"xray-secret-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(outside);
        await File.WriteAllTextAsync(Path.Combine(outside, "secret.txt"), "top secret");

        try
        {
            var provider = new LocalRepositoryProvider();
            var connection = new RepositoryConnectionInfo("LOCAL", RootPath: root);
            var escapingRelativePath = Path.GetRelativePath(root, Path.Combine(outside, "secret.txt")).Replace('\\', '/');

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GetFileContentAsync(connection, "main", escapingRelativePath, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(outside, recursive: true);
        }
    }
}
