using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/security-findings")]
public class SecurityController : ControllerBase
{
    private readonly AppDbContext _db;

    public SecurityController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SecurityFindingResponse>>> List(Guid projectId, CancellationToken ct)
    {
        var severityByRule = await _db.SecurityRules.ToDictionaryAsync(r => r.SecurityRuleId, r => r, ct);
        var severityCodes = await _db.SecuritySeverities.ToDictionaryAsync(s => s.Id, s => s.Code, ct);

        var findings = await _db.SecurityFindings
            .Where(f => _db.SecurityScans.Any(s => s.SecurityScanId == f.SecurityScanId &&
                                                     _db.Analyses.Any(a => a.AnalysisId == s.AnalysisId && a.ProjectId == projectId)))
            .OrderByDescending(f => f.CreatedAtUtc)
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
