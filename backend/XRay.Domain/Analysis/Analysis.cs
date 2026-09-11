using XRay.Domain.Changes;
using XRay.Domain.Graph;
using XRay.Domain.Identity;
using XRay.Domain.Ingestion;
using XRay.Domain.Projects;

namespace XRay.Domain.Analysis;

public class Analysis
{
    public Guid AnalysisId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? ChangeId { get; set; }
    public Change? Change { get; set; }
    public Guid? GraphSnapshotId { get; set; }
    public GraphSnapshot? GraphSnapshot { get; set; }
    public Guid? EnvironmentId { get; set; }
    public ProjectEnvironment? Environment { get; set; }
    public byte AnalysisStatusId { get; set; }
    public byte? OverallRiskStateId { get; set; }
    public short MaxDepth { get; set; } = 6;
    public decimal ConfidenceThreshold { get; set; } = 0.7000m;
    public bool IncludeSecurityScan { get; set; } = true;
    public bool IncludeAIExplanation { get; set; } = true;
    public bool IncludeExternalApis { get; set; } = true;
    public bool TraceTransitive { get; set; } = true;
    public bool IsEvidenceComplete { get; set; }
    public bool IsDeterministicComplete { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid RequestedByUserId { get; set; }
    public UserAccount? RequestedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class AnalysisTarget
{
    public Guid AnalysisTargetId { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis? Analysis { get; set; }
    public Guid? CodeFileId { get; set; }
    public CodeFile? CodeFile { get; set; }
    public Guid? GraphNodeId { get; set; }
    public GraphNode? GraphNode { get; set; }
    public string? TargetReasonCode { get; set; }
}

public class AnalysisScope
{
    public Guid AnalysisScopeId { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis? Analysis { get; set; }
    public bool ScanDirectDependencies { get; set; }
    public bool TraceTransitiveDependencies { get; set; }
    public bool IncludeExternalBindings { get; set; }
    public bool RunStaticSecurityAnalysis { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class AnalysisProgress
{
    public Guid AnalysisProgressId { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis? Analysis { get; set; }
    public byte AnalysisStageId { get; set; }
    public string StatusCode { get; set; } = default!;
    public decimal PercentComplete { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? ComponentsTraced { get; set; }
    public int? EstimatedChains { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AnalysisTelemetry
{
    public Guid AnalysisTelemetryId { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis? Analysis { get; set; }
    public byte? AnalysisStageId { get; set; }
    public string LogLevelCode { get; set; } = default!;
    public string Message { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}

public class AnalysisNodeResult
{
    public Guid AnalysisNodeResultId { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis? Analysis { get; set; }
    public Guid GraphNodeId { get; set; }
    public GraphNode? GraphNode { get; set; }
    public byte RiskStateId { get; set; }
    public int? Distance { get; set; }
    public decimal? MinPathConfidence { get; set; }
    public string? RuleCode { get; set; }
    public string? ReasonCode { get; set; }
    public bool IsDirectlyChanged { get; set; }
    public bool IsSecurityAffected { get; set; }
    public bool IsRuntimeResolved { get; set; }
    public bool IsBeyondDepth { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ImpactPath
{
    public Guid ImpactPathId { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis? Analysis { get; set; }
    public Guid TargetNodeId { get; set; }
    public GraphNode? TargetNode { get; set; }
    public Guid ResultNodeId { get; set; }
    public GraphNode? ResultNode { get; set; }
    public int PathDistance { get; set; }
    public decimal PathConfidence { get; set; }
    public bool ContainsRuntimeEdge { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<ImpactPathEdge> Edges { get; set; } = new();
}

public class ImpactPathEdge
{
    public Guid ImpactPathId { get; set; }
    public ImpactPath? ImpactPath { get; set; }
    public int SequenceNo { get; set; }
    public Guid GraphEdgeId { get; set; }
    public GraphEdge? GraphEdge { get; set; }
}
