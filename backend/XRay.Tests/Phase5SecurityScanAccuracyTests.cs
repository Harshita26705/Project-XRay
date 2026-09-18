using Microsoft.EntityFrameworkCore;
using XRay.Api.Services;
using XRay.Domain.Analysis;
using XRay.Domain.Reference;
using XRay.Infrastructure.Persistence;

namespace XRay.Tests;

public class Phase5SecurityScanAccuracyTests
{
    private static async Task<(AppDbContext Db, Guid AnalysisId)> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.SecuritySeverities.AddRange(
            new SecuritySeverity { Id = 1, Code = "CRITICAL", DisplayName = "Critical", SortOrder = 1 },
            new SecuritySeverity { Id = 2, Code = "HIGH", DisplayName = "High", SortOrder = 2 },
            new SecuritySeverity { Id = 3, Code = "MEDIUM", DisplayName = "Medium", SortOrder = 3 },
            new SecuritySeverity { Id = 4, Code = "LOW", DisplayName = "Low", SortOrder = 4 });

        var analysisId = Guid.NewGuid();
        db.Analyses.Add(new Analysis
        {
            AnalysisId = analysisId, OrganizationId = Guid.NewGuid(), ProjectId = Guid.NewGuid(),
            AnalysisStatusId = 1, RequestedByUserId = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return (db, analysisId);
    }

    [Fact]
    public async Task RunScanAsync_ReportsEveryMatchNotJustTheFirst()
    {
        var (db, analysisId) = await SeedAsync();
        await using var _db = db;
        var service = new SecurityScanService(db);

        var content = "var password = \"firstsecret\"; var apiKey = \"secondsecret\";";
        var files = new List<(Guid? GraphNodeId, string? FilePath, string Content)> { (null, "Demo.cs", content) };

        var scanId = await service.RunScanAsync(analysisId, files, CancellationToken.None);

        var findings = await db.SecurityFindings.Where(f => f.SecurityScanId == scanId).ToListAsync();
        Assert.Equal(2, findings.Count);
    }

    [Fact]
    public async Task RunScanAsync_DetectsJsonStyleSecretsButIgnoresPlaceholders()
    {
        var (db, analysisId) = await SeedAsync();
        await using var _db = db;
        var service = new SecurityScanService(db);

        var placeholderJson = "{ \"Gemini\": { \"ApiKey\": \"REPLACE-WITH-YOUR-GEMINI-API-KEY\" } }";
        var placeholderFiles = new List<(Guid? GraphNodeId, string? FilePath, string Content)> { (null, "appsettings.json", placeholderJson) };
        var placeholderScanId = await service.RunScanAsync(analysisId, placeholderFiles, CancellationToken.None);
        Assert.Empty(await db.SecurityFindings.Where(f => f.SecurityScanId == placeholderScanId).ToListAsync());

        var realJson = "{ \"Gemini\": { \"ApiKey\": \"REPLACE-WITH-YOUR-GEMINI-API-KEY\" } }";
        var realFiles = new List<(Guid? GraphNodeId, string? FilePath, string Content)> { (null, "appsettings.json", realJson) };
        var realScanId = await service.RunScanAsync(analysisId, realFiles, CancellationToken.None);
        Assert.Single(await db.SecurityFindings.Where(f => f.SecurityScanId == realScanId).ToListAsync());
    }

    [Fact]
    public async Task RunScanAsync_DetectsRealDotNetWeakCryptoClassNames()
    {
        var (db, analysisId) = await SeedAsync();
        await using var _db = db;
        var service = new SecurityScanService(db);

        var content = "using var md5 = new MD5CryptoServiceProvider(); using var sha1 = new SHA1Managed();";
        var files = new List<(Guid? GraphNodeId, string? FilePath, string Content)> { (null, "Crypto.cs", content) };

        var scanId = await service.RunScanAsync(analysisId, files, CancellationToken.None);

        var findings = await db.SecurityFindings.Where(f => f.SecurityScanId == scanId).ToListAsync();
        Assert.Equal(2, findings.Count);
    }
}
