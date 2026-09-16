using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XRay.Api.Contracts;
using XRay.Api.Services;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reports;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(ReportService reports, ICurrentUserService currentUser)
    {
        _reports = reports;
        _currentUser = currentUser;
    }

    [HttpPost("analyses/{analysisId:guid}/reports")]
    public async Task<ActionResult<ReportResponse>> Generate(Guid analysisId, CancellationToken ct)
    {
        var user = await _currentUser.GetOrProvisionUserAsync(User, ct);
        try
        {
            var report = await _reports.GenerateAsync(analysisId, user.UserId, ct);
            return CreatedAtAction(nameof(Get), new { reportId = report.ReportId }, report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("reports/{reportId:guid}")]
    public async Task<ActionResult<ReportResponse>> Get(Guid reportId, CancellationToken ct)
    {
        var report = await _reports.GetAsync(reportId, ct);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpGet("projects/{projectId:guid}/reports")]
    public async Task<ActionResult<IReadOnlyList<ReportResponse>>> List(Guid projectId, CancellationToken ct) =>
        Ok(await _reports.ListAsync(projectId, ct));

    [HttpGet("reports/{reportId:guid}/export")]
    public async Task<ActionResult> Export(Guid reportId, [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        var report = await _reports.GetAsync(reportId, ct);
        if (report is null) return NotFound();

        var payload = string.Join("\n\n", report.Sections.Select(s => $"<h2>{System.Net.WebUtility.HtmlEncode(s.Title ?? s.SectionType)}</h2><p>{System.Net.WebUtility.HtmlEncode(s.Content ?? string.Empty)}</p>"));
        var html = $"<html><body><h1>{System.Net.WebUtility.HtmlEncode(report.Title)}</h1>{payload}</body></html>";

        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Section,Title,Content");
            foreach (var section in report.Sections)
            {
                var sectionValue = EscapeCsv(section.SectionType);
                var titleValue = EscapeCsv(section.Title ?? string.Empty);
                var contentValue = EscapeCsv(section.Content ?? string.Empty);
                csv.AppendLine($"{sectionValue},{titleValue},{contentValue}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"{report.Title}.csv");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8", $"{report.Title}.html");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
