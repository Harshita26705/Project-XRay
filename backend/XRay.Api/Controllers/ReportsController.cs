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
}
