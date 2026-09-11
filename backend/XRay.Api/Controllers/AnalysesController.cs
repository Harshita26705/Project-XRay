using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Api.Services;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class AnalysesController : ControllerBase
{
    private readonly AnalysisService _analyses;
    private readonly ICurrentUserService _currentUser;
    private readonly AppDbContext _db;

    public AnalysesController(AnalysisService analyses, ICurrentUserService currentUser, AppDbContext db)
    {
        _analyses = analyses;
        _currentUser = currentUser;
        _db = db;
    }

    [HttpPost("analyses")]
    public async Task<ActionResult<AnalysisResponse>> Create(CreateAnalysisRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetOrProvisionUserAsync(User, ct);
        var org = await _currentUser.GetOrProvisionOrganizationAsync(user, ct);
        try
        {
            var created = await _analyses.CreateAndRunAsync(org.OrganizationId, user.UserId, request, ct);
            return CreatedAtAction(nameof(Get), new { analysisId = created.AnalysisId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("analyses/{analysisId:guid}")]
    public async Task<ActionResult<AnalysisResponse>> Get(Guid analysisId, CancellationToken ct)
    {
        var analysis = await _analyses.GetAsync(analysisId, ct);
        return analysis is null ? NotFound() : Ok(analysis);
    }

    [HttpGet("projects/{projectId:guid}/analyses")]
    public async Task<ActionResult<IReadOnlyList<AnalysisResponse>>> List(Guid projectId, CancellationToken ct) =>
        Ok(await _analyses.ListAsync(projectId, ct));

    [HttpGet("analyses/{analysisId:guid}/progress")]
    public async Task<ActionResult<AnalysisProgressResponse>> Progress(Guid analysisId, CancellationToken ct)
    {
        // The engine currently runs synchronously to completion, so progress is always reported as
        // 100% complete by the time this is queryable. Kept as a real endpoint so the Analysis
        // Progress screen has a stable contract to poll once background job dispatch lands.
        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.AnalysisId == analysisId, ct);
        if (analysis is null) return NotFound();
        var statusCode = await _db.AnalysisStatuses.Where(s => s.Id == analysis.AnalysisStatusId).Select(s => s.Code).FirstAsync(ct);
        return Ok(new AnalysisProgressResponse("GENERATING_REPORT", statusCode, statusCode == "COMPLETED" ? 100m : 50m, null, null));
    }

    [HttpGet("analyses/{analysisId:guid}/evidence")]
    public async Task<ActionResult<IReadOnlyList<EvidenceResponse>>> Evidence(Guid analysisId, CancellationToken ct)
    {
        var evidenceTypes = await _db.EvidenceTypes.ToDictionaryAsync(e => e.Id, e => e.Code, ct);
        var items = await _db.Evidences.Where(e => e.AnalysisId == analysisId).ToListAsync(ct);
        var codeFiles = await _db.CodeFiles.Where(f => items.Select(i => i.CodeFileId).Contains(f.CodeFileId)).ToDictionaryAsync(f => f.CodeFileId, f => f.RelativePath, ct);

        var result = items.Select(e => new EvidenceResponse(
            e.EvidenceId, evidenceTypes.GetValueOrDefault(e.EvidenceTypeId, "PARSER_EVIDENCE"), e.Title, e.Description,
            e.CodeFileId is not null ? codeFiles.GetValueOrDefault(e.CodeFileId.Value) : null,
            e.SourceLineStart, e.SourceLineEnd, e.Confidence, e.SourceSnippet)).ToList();
        return Ok(result);
    }

    [HttpGet("analyses/{analysisId:guid}/security-findings")]
    public async Task<ActionResult<IReadOnlyList<SecurityFindingResponse>>> SecurityFindings(Guid analysisId, CancellationToken ct)
    {
        var severityByRule = await _db.SecurityRules.ToDictionaryAsync(r => r.SecurityRuleId, r => r, ct);
        var severityCodes = await _db.SecuritySeverities.ToDictionaryAsync(s => s.Id, s => s.Code, ct);
        var componentTypes = await _db.ComponentTypes.ToDictionaryAsync(c => c.Id, c => c.Code, ct);

        var findings = await _db.SecurityFindings
            .Where(f => _db.SecurityScans.Any(s => s.SecurityScanId == f.SecurityScanId && s.AnalysisId == analysisId))
            .ToListAsync(ct);

        var nodes = await _db.GraphNodes.Where(n => findings.Select(f => f.GraphNodeId).Contains(n.GraphNodeId)).ToDictionaryAsync(n => n.GraphNodeId, ct);
        var codeFiles = await _db.CodeFiles.Where(f => findings.Select(x => x.CodeFileId).Contains(f.CodeFileId)).ToDictionaryAsync(f => f.CodeFileId, f => f.RelativePath, ct);

        var result = findings.Select(f =>
        {
            var severity = severityByRule.TryGetValue(f.SecurityRuleId, out var rule) ? severityCodes.GetValueOrDefault(rule.SecuritySeverityId, "INFO") : "INFO";
            nodes.TryGetValue(f.GraphNodeId ?? Guid.Empty, out var node);
            var filePath = f.CodeFileId is not null ? codeFiles.GetValueOrDefault(f.CodeFileId.Value) : null;
            return new SecurityFindingResponse(f.SecurityFindingId, f.Title, severity, node?.DisplayName, filePath, f.SourceLineStart, f.StatusCode, f.Description, f.Remediation);
        }).ToList();

        return Ok(result);
    }
}
