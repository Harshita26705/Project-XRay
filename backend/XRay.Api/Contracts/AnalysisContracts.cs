namespace XRay.Api.Contracts;

public record CreateChangeRequest(
    string ChangeType, // PULL_REQUEST | WORK_ITEM | MANUAL
    string Title,
    string? Description,
    string? ExternalChangeId,
    IReadOnlyList<string> ChangedFilePaths);

public record ChangeResponse(
    Guid ChangeId,
    string ChangeType,
    string Title,
    string? Description,
    string? Author,
    DateTime CreatedAtUtc,
    string? RiskState,
    int AffectedComponents,
    string Status,
    IReadOnlyList<string> ChangedFilePaths);

public record AnalysisScopeRequest(
    bool ScanDirectDependencies = true,
    bool TraceTransitiveDependencies = true,
    bool IncludeExternalBindings = true,
    bool RunStaticSecurityAnalysis = true);

public record CreateAnalysisRequest(Guid ChangeId, AnalysisScopeRequest? Scope, string? BranchName);

public record AnalysisNodeResultResponse(
    Guid GraphNodeId,
    string DisplayName,
    string ComponentType,
    string RiskState,
    int? Distance,
    decimal? MinPathConfidence,
    string? RuleCode,
    bool IsDirectlyChanged);

public record AnalysisResponse(
    Guid AnalysisId,
    Guid ChangeId,
    string ChangeTitle,
    string Status,
    string? OverallRiskState,
    string? BranchName,
    int CriticalCount,
    int RiskyCount,
    int SafeCount,
    int UnknownCount,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyList<AnalysisNodeResultResponse> Nodes);

public record AnalysisProgressResponse(string StageCode, string StatusCode, decimal PercentComplete, int? ComponentsTraced, int? EstimatedChains);

public record EvidenceResponse(
    Guid EvidenceId,
    string EvidenceType,
    string Title,
    string? Description,
    string? FilePath,
    int? LineStart,
    int? LineEnd,
    decimal? Confidence,
    string? SourceSnippet);

public record SecurityFindingResponse(
    Guid SecurityFindingId,
    string Title,
    string Severity,
    string? Component,
    string? FilePath,
    int? LineStart,
    string Status,
    string? Description,
    string? Remediation);

public record ExplainRequest(string? Focus);

public record ExplainResponse(string Summary, IReadOnlyList<string> KeyPoints, bool Degraded);
