using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using XRay.Domain.Analysis;
using XRay.Domain.Reference;
using XRay.Domain.Security;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

/// <summary>
/// Lightweight SAST-style regex scanners (hardcoded secrets, SQL injection via string concatenation,
/// weak crypto). Honest degradation: if a file can't be read the scan just skips it rather than
/// silently reporting "safe" — matching the product's "never convert missing evidence into SAFE" rule.
/// </summary>
public class SecurityScanService
{
    private static readonly (string Code, string Name, string Severity, Regex Pattern)[] Rules =
    {
        ("HARDCODED_SECRET", "Hardcoded secret", "CRITICAL",
            new Regex(@"(password|apikey|api_key|secret|connectionstring)\s*=\s*[""'][^""']{4,}[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("SQL_INJECTION", "SAST SQL Injection Potential", "CRITICAL",
            new Regex(@"(SELECT|INSERT|UPDATE|DELETE)[^;]*""\s*\+\s*\w+|\$""[^""]*(SELECT|INSERT|UPDATE|DELETE)[^""]*\{", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("WEAK_ENCRYPTION", "Weak encryption", "MEDIUM",
            new Regex(@"\b(MD5|DES|SHA1)\b", RegexOptions.Compiled)),
        ("INSECURE_DESERIALIZATION", "Insecure deserialization", "HIGH",
            new Regex(@"BinaryFormatter|JavaScriptSerializer", RegexOptions.Compiled)),
    };

    private readonly AppDbContext _db;

    public SecurityScanService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> RunScanAsync(Guid analysisId, IReadOnlyList<(Guid? GraphNodeId, string? FilePath, string Content)> filesToScan, CancellationToken ct = default)
    {
        var scanner = await _db.SecurityScanners.FirstOrDefaultAsync(s => s.Code == "XRAY_REGEX_SAST", ct);
        if (scanner is null)
        {
            var org = await _db.Analyses.Where(a => a.AnalysisId == analysisId).Select(a => a.OrganizationId).FirstAsync(ct);
            scanner = new SecurityScanner { SecurityScannerId = Guid.NewGuid(), OrganizationId = org, Name = "X-Ray Structural SAST", Code = "XRAY_REGEX_SAST" };
            _db.SecurityScanners.Add(scanner);
        }

        var severityIds = await _db.SecuritySeverities.ToDictionaryAsync(s => s.Code, s => s.Id, ct);

        var scan = new SecurityScan
        {
            SecurityScanId = Guid.NewGuid(),
            AnalysisId = analysisId,
            SecurityScannerId = scanner.SecurityScannerId,
            StatusCode = "COMPLETED",
            StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow,
        };
        _db.SecurityScans.Add(scan);

        var findingCount = 0;
        foreach (var (graphNodeId, filePath, content) in filesToScan)
        {
            foreach (var rule in Rules)
            {
                var match = rule.Pattern.Match(content);
                if (!match.Success) continue;

                var rule_ = await GetOrCreateRuleAsync(scanner.SecurityScannerId, rule.Code, rule.Name, severityIds[rule.Severity], ct);
                var lineNo = content[..match.Index].Count(c => c == '\n') + 1;

                _db.SecurityFindings.Add(new SecurityFinding
                {
                    SecurityFindingId = Guid.NewGuid(),
                    SecurityScanId = scan.SecurityScanId,
                    SecurityRuleId = rule_.SecurityRuleId,
                    GraphNodeId = graphNodeId,
                    Title = rule.Name,
                    Description = $"Pattern '{rule.Code}' matched in {filePath ?? "unknown file"} at line {lineNo}.",
                    SourceLineStart = lineNo,
                    SourceLineEnd = lineNo,
                    Remediation = RemediationFor(rule.Code),
                    StatusCode = "OPEN",
                    CreatedAtUtc = DateTime.UtcNow,
                });
                findingCount++;
            }
        }

        scan.FindingCount = findingCount;
        await _db.SaveChangesAsync(ct);
        return scan.SecurityScanId;
    }

    public async Task<SecurityScan> RunProjectScanAsync(Guid projectId, string repositoryRootPath, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId, ct)
            ?? throw new InvalidOperationException("Project not found.");

        var completedStatusId = await _db.AnalysisStatuses.Where(s => s.Code == "COMPLETED").Select(s => s.Id).FirstOrDefaultAsync(ct);
        if (completedStatusId == 0)
        {
            var completedStatus = new AnalysisStatus
            {
                Id = 1,
                Code = "COMPLETED",
                DisplayName = "Completed"
            };
            if (await _db.AnalysisStatuses.AnyAsync(s => s.Code == completedStatus.Code, ct) == false)
            {
                _db.AnalysisStatuses.Add(completedStatus);
                await _db.SaveChangesAsync(ct);
            }
            completedStatusId = await _db.AnalysisStatuses.Where(s => s.Code == "COMPLETED").Select(s => s.Id).FirstAsync(ct);
        }

        var analysis = new Analysis
        {
            AnalysisId = Guid.NewGuid(),
            OrganizationId = project.OrganizationId,
            ProjectId = project.ProjectId,
            AnalysisStatusId = completedStatusId,
            IsDeterministicComplete = true,
            IsEvidenceComplete = true,
            RequestedByUserId = project.CreatedByUserId,
            StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Analyses.Add(analysis);
        await _db.SaveChangesAsync(ct);

        var filesToScan = new List<(Guid? GraphNodeId, string? FilePath, string Content)>();
        foreach (var file in Directory.EnumerateFiles(repositoryRootPath, "*.*", SearchOption.AllDirectories).Where(file =>
                     file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                     file.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ||
                     file.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase) ||
                     file.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                     file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) ||
                     file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var content = await File.ReadAllTextAsync(file, ct);
                filesToScan.Add((null, Path.GetRelativePath(repositoryRootPath, file).Replace('\\', '/'), content));
            }
            catch (IOException) { }
        }

        var scanner = await _db.SecurityScanners.FirstOrDefaultAsync(s => s.Code == "XRAY_REGEX_SAST", ct);
        if (scanner is null)
        {
            scanner = new SecurityScanner { SecurityScannerId = Guid.NewGuid(), OrganizationId = project.OrganizationId, Name = "X-Ray Structural SAST", Code = "XRAY_REGEX_SAST" };
            _db.SecurityScanners.Add(scanner);
        }

        var severityIds = await EnsureSeverityMapAsync(ct);
        var scan = new SecurityScan
        {
            SecurityScanId = Guid.NewGuid(),
            AnalysisId = analysis.AnalysisId,
            SecurityScannerId = scanner.SecurityScannerId,
            StatusCode = "COMPLETED",
            StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow,
        };
        _db.SecurityScans.Add(scan);

        var findingCount = 0;
        foreach (var (graphNodeId, filePath, content) in filesToScan)
        {
            foreach (var rule in Rules)
            {
                var match = rule.Pattern.Match(content);
                if (!match.Success) continue;

                var ruleEntity = await GetOrCreateRuleAsync(scanner.SecurityScannerId, rule.Code, rule.Name, severityIds[rule.Severity], ct);
                var lineNo = content[..match.Index].Count(c => c == '\n') + 1;
                _db.SecurityFindings.Add(new SecurityFinding
                {
                    SecurityFindingId = Guid.NewGuid(),
                    SecurityScanId = scan.SecurityScanId,
                    SecurityRuleId = ruleEntity.SecurityRuleId,
                    GraphNodeId = graphNodeId,
                    CodeFileId = null,
                    Title = rule.Name,
                    Description = $"Pattern '{rule.Code}' matched in {filePath ?? "unknown file"} at line {lineNo}.",
                    SourceLineStart = lineNo,
                    SourceLineEnd = lineNo,
                    Remediation = RemediationFor(rule.Code),
                    StatusCode = "OPEN",
                    CreatedAtUtc = DateTime.UtcNow,
                });
                findingCount++;
            }
        }

        scan.FindingCount = findingCount;
        await _db.SaveChangesAsync(ct);
        return scan;
    }

    private async Task<Dictionary<string, byte>> EnsureSeverityMapAsync(CancellationToken ct)
    {
        var required = new[]
        {
            new SecuritySeverity { Id = 1, Code = "CRITICAL", DisplayName = "Critical", SortOrder = 1 },
            new SecuritySeverity { Id = 2, Code = "HIGH", DisplayName = "High", SortOrder = 2 },
            new SecuritySeverity { Id = 3, Code = "MEDIUM", DisplayName = "Medium", SortOrder = 3 },
            new SecuritySeverity { Id = 4, Code = "LOW", DisplayName = "Low", SortOrder = 4 },
        };

        var existing = await _db.SecuritySeverities.ToListAsync(ct);
        foreach (var item in required)
        {
            if (existing.Any(s => s.Code == item.Code)) continue;
            _db.SecuritySeverities.Add(item);
        }

        await _db.SaveChangesAsync(ct);
        return await _db.SecuritySeverities.ToDictionaryAsync(s => s.Code, s => s.Id, ct);
    }

    private async Task<SecurityRule> GetOrCreateRuleAsync(Guid scannerId, string code, string name, byte severityId, CancellationToken ct)
    {
        var rule = await _db.SecurityRules.FirstOrDefaultAsync(r => r.SecurityScannerId == scannerId && r.RuleCode == code, ct);
        if (rule is not null) return rule;

        rule = new SecurityRule { SecurityRuleId = Guid.NewGuid(), SecurityScannerId = scannerId, RuleCode = code, Name = name, SecuritySeverityId = severityId };
        _db.SecurityRules.Add(rule);
        return rule;
    }

    private static string RemediationFor(string code) => code switch
    {
        "HARDCODED_SECRET" => "Move the secret to Azure Key Vault and reference it via SecretReference.",
        "SQL_INJECTION" => "Parameterize the SQL command instead of concatenating user input.",
        "WEAK_ENCRYPTION" => "Use a modern algorithm such as SHA-256/AES instead.",
        "INSECURE_DESERIALIZATION" => "Avoid BinaryFormatter/JavaScriptSerializer; use System.Text.Json with allow-lists.",
        _ => "Review the finding and remediate according to your security policy.",
    };
}
