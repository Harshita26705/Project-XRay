namespace XRay.Api.Contracts;

public record IntegrationConnectionResponse(
    Guid IntegrationConnectionId,
    string Provider,
    string DisplayName,
    string Status,
    DateTime? LastTestedAtUtc,
    bool IsEnabled,
    string? ExternalBaseUrl);

public record CreateIntegrationRequest(string Provider, string DisplayName, string? ExternalBaseUrl, string? ExternalTenantId);

public record AiConfigurationResponse(string Provider, string DeploymentStatus, string? ActiveModel, string KnowledgeLayer);

public record NotificationChannelResponse(Guid NotificationChannelId, string ChannelType, string DisplayName, bool IsEnabled);

public record NotificationRuleResponse(Guid NotificationRuleId, string EventType, Guid ChannelId, string ChannelType, bool IsEnabled);

public record UpdateNotificationRuleRequest(bool IsEnabled);

public record OrganizationSettingsResponse(
    string OrganizationName,
    Guid? DefaultTargetProjectId,
    bool IncludeSecurityScan,
    bool IncludeAiExplanation,
    bool AutoAnalyzePullRequests);

public record UpdateOrganizationSettingsRequest(
    string OrganizationName,
    Guid? DefaultTargetProjectId,
    bool IncludeSecurityScan,
    bool IncludeAiExplanation,
    bool AutoAnalyzePullRequests);

public record ReportResponse(
    Guid ReportId,
    Guid AnalysisId,
    string Title,
    string Format,
    DateTime CreatedAtUtc,
    string? OverallRiskState,
    IReadOnlyList<ReportSectionResponse> Sections);

public record ReportSectionResponse(string SectionType, string Title, string? Content, int SortOrder);
