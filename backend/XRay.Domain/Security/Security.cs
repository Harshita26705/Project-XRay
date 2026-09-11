using XRay.Domain.Analysis;
using XRay.Domain.EvidenceModel;
using XRay.Domain.Graph;
using XRay.Domain.Identity;
using XRay.Domain.Ingestion;

namespace XRay.Domain.Security;

public class SecurityScanner
{
    public Guid SecurityScannerId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Version { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class SecurityRule
{
    public Guid SecurityRuleId { get; set; }
    public Guid SecurityScannerId { get; set; }
    public SecurityScanner? SecurityScanner { get; set; }
    public string RuleCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? CategoryCode { get; set; }
    public byte SecuritySeverityId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SecurityScan
{
    public Guid SecurityScanId { get; set; }
    public Guid AnalysisId { get; set; }
    public XRay.Domain.Analysis.Analysis? Analysis { get; set; }
    public Guid SecurityScannerId { get; set; }
    public SecurityScanner? SecurityScanner { get; set; }
    public string StatusCode { get; set; } = default!;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int FindingCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SecurityFinding
{
    public Guid SecurityFindingId { get; set; }
    public Guid SecurityScanId { get; set; }
    public SecurityScan? SecurityScan { get; set; }
    public Guid SecurityRuleId { get; set; }
    public SecurityRule? SecurityRule { get; set; }
    public Guid? GraphNodeId { get; set; }
    public GraphNode? GraphNode { get; set; }
    public Guid? CodeFileId { get; set; }
    public CodeFile? CodeFile { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string StatusCode { get; set; } = "OPEN";
    public int? SourceLineStart { get; set; }
    public int? SourceLineEnd { get; set; }
    public string? Remediation { get; set; }
    public string? Fingerprint { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class SecurityFindingEvidence
{
    public Guid SecurityFindingId { get; set; }
    public SecurityFinding? SecurityFinding { get; set; }
    public Guid EvidenceId { get; set; }
    public Evidence? Evidence { get; set; }
}
