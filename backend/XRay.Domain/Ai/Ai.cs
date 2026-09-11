using XRay.Domain.Analysis;
using XRay.Domain.EvidenceModel;
using XRay.Domain.Identity;
using XRay.Domain.Projects;
using XRay.Domain.Recommendations;
using XRay.Domain.Security;

namespace XRay.Domain.Ai;

public class AIProvider
{
    public Guid AIProviderId { get; set; }
    public string Code { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class AIConfiguration
{
    public Guid AIConfigurationId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid AIProviderId { get; set; }
    public AIProvider? AIProvider { get; set; }
    public string? DeploymentName { get; set; }
    public string? ModelName { get; set; }
    public decimal? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class PromptTemplate
{
    public Guid PromptTemplateId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Name { get; set; } = default!;
    public string TemplateTypeCode { get; set; } = default!;
    public string? SystemPrompt { get; set; }
    public string UserPrompt { get; set; } = default!;
    public int VersionNo { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public UserAccount? CreatedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class AIGeneration
{
    public Guid AIGenerationId { get; set; }
    public Guid AnalysisId { get; set; }
    public XRay.Domain.Analysis.Analysis? Analysis { get; set; }
    public Guid AIConfigurationId { get; set; }
    public AIConfiguration? AIConfiguration { get; set; }
    public Guid? PromptTemplateId { get; set; }
    public PromptTemplate? PromptTemplate { get; set; }
    public string PurposeCode { get; set; } = default!;
    public string StatusCode { get; set; } = default!;
    public string? InputHash { get; set; }
    public string? OutputText { get; set; }
    public string? ValidatedOutput { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public long? LatencyMs { get; set; }
    public bool Degraded { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>Prevents AI output from becoming an independent source of truth — every generation must reference verified analysis facts.</summary>
public class AIOutputReference
{
    public Guid AIOutputReferenceId { get; set; }
    public Guid AIGenerationId { get; set; }
    public AIGeneration? AIGeneration { get; set; }
    public Guid? EvidenceId { get; set; }
    public Evidence? Evidence { get; set; }
    public Guid? AnalysisNodeResultId { get; set; }
    public AnalysisNodeResult? AnalysisNodeResult { get; set; }
    public Guid? SecurityFindingId { get; set; }
    public SecurityFinding? SecurityFinding { get; set; }
    public Guid? RecommendationId { get; set; }
    public Recommendation? Recommendation { get; set; }
}
