using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Reporting;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

public class ReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReportResponse> GenerateAsync(Guid analysisId, Guid? generatedByUserId, CancellationToken ct = default)
    {
        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.AnalysisId == analysisId, ct)
                       ?? throw new InvalidOperationException("Analysis not found.");
        var change = analysis.ChangeId is null ? null : await _db.Changes.FirstOrDefaultAsync(c => c.ChangeId == analysis.ChangeId, ct);
        var nodeResults = await _db.AnalysisNodeResults.Where(n => n.AnalysisId == analysisId).ToListAsync(ct);
        var riskStates = await _db.RiskStates.ToDictionaryAsync(r => r.Id, r => r.Code, ct);
        var findings = await _db.SecurityFindings.Where(f => _db.SecurityScans.Any(s => s.SecurityScanId == f.SecurityScanId && s.AnalysisId == analysisId)).ToListAsync(ct);

        var critical = nodeResults.Count(n => riskStates.GetValueOrDefault(n.RiskStateId) == "CRITICAL");
        var risky = nodeResults.Count(n => riskStates.GetValueOrDefault(n.RiskStateId) == "RISKY");

        var report = new Report
        {
            ReportId = Guid.NewGuid(),
            AnalysisId = analysisId,
            Title = change?.Title ?? "Analysis Report",
            FormatCode = "MARKDOWN",
            GeneratedByUserId = generatedByUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Reports.Add(report);

        var sections = new List<ReportSection>
        {
            new()
            {
                ReportSectionId = Guid.NewGuid(), ReportId = report.ReportId, SortOrder = 1,
                SectionTypeCode = "EXECUTIVE_SUMMARY", Title = "Executive Summary",
                Content = change?.Description ?? "Structural analysis completed against the current dependency graph.",
            },
            new()
            {
                ReportSectionId = Guid.NewGuid(), ReportId = report.ReportId, SortOrder = 2,
                SectionTypeCode = "BLAST_RADIUS", Title = "Blast Radius",
                Content = $"{critical} critical component(s), {risky} risky transitive impact(s).",
            },
            new()
            {
                ReportSectionId = Guid.NewGuid(), ReportId = report.ReportId, SortOrder = 3,
                SectionTypeCode = "SECURITY", Title = "Security & Vulnerabilities",
                Content = findings.Count == 0 ? "No security findings were raised for this analysis."
                    : $"{findings.Count} finding(s), most notably: {findings[0].Title}.",
            },
            new()
            {
                ReportSectionId = Guid.NewGuid(), ReportId = report.ReportId, SortOrder = 4,
                SectionTypeCode = "RECOMMENDATIONS", Title = "Recommendations",
                Content = BuildRecommendations(findings.Count > 0),
            },
        };
        _db.ReportSections.AddRange(sections);
        await _db.SaveChangesAsync(ct);

        return new ReportResponse(report.ReportId, analysisId, report.Title, report.FormatCode, report.CreatedAtUtc,
            sections.Select(s => new ReportSectionResponse(s.SectionTypeCode, s.Title, s.Content, s.SortOrder)).ToList());
    }

    public async Task<IReadOnlyList<ReportResponse>> ListAsync(Guid projectId, CancellationToken ct = default)
    {
        var reports = await _db.Reports
            .Where(r => _db.Analyses.Any(a => a.AnalysisId == r.AnalysisId && a.ProjectId == projectId))
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        var result = new List<ReportResponse>();
        foreach (var r in reports)
        {
            var sections = await _db.ReportSections.Where(s => s.ReportId == r.ReportId).OrderBy(s => s.SortOrder).ToListAsync(ct);
            result.Add(new ReportResponse(r.ReportId, r.AnalysisId, r.Title, r.FormatCode, r.CreatedAtUtc,
                sections.Select(s => new ReportSectionResponse(s.SectionTypeCode, s.Title, s.Content, s.SortOrder)).ToList()));
        }
        return result;
    }

    public async Task<ReportResponse?> GetAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.ReportId == reportId, ct);
        if (report is null) return null;
        var sections = await _db.ReportSections.Where(s => s.ReportId == reportId).OrderBy(s => s.SortOrder).ToListAsync(ct);
        return new ReportResponse(report.ReportId, report.AnalysisId, report.Title, report.FormatCode, report.CreatedAtUtc,
            sections.Select(s => new ReportSectionResponse(s.SectionTypeCode, s.Title, s.Content, s.SortOrder)).ToList());
    }

    private static string BuildRecommendations(bool hasFindings) => hasFindings
        ? "Parameterize raw SQL strings inside repository handlers immediately.\nValidate input payloads inside controller schemas before service entry.\nRun the unit test suite focusing on mock-validation boundaries."
        : "No immediate remediation required; continue monitoring on the next analysis cycle.";
}
