using XRay.Domain.Analysis;
using XRay.Domain.Changes;
using XRay.Domain.Identity;
using XRay.Domain.Integrations;
using XRay.Domain.Projects;
using XRay.Domain.Security;

namespace XRay.Domain.Notifications;

public class NotificationChannel
{
    public Guid NotificationChannelId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid? IntegrationConnectionId { get; set; }
    public IntegrationConnection? IntegrationConnection { get; set; }
    public string ChannelTypeCode { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}

public class NotificationRule
{
    public Guid NotificationRuleId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public byte NotificationEventTypeId { get; set; }
    public Guid NotificationChannelId { get; set; }
    public NotificationChannel? NotificationChannel { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class NotificationDelivery
{
    public Guid NotificationDeliveryId { get; set; }
    public Guid NotificationRuleId { get; set; }
    public NotificationRule? NotificationRule { get; set; }
    public Guid? AnalysisId { get; set; }
    public XRay.Domain.Analysis.Analysis? Analysis { get; set; }
    public Guid? SecurityFindingId { get; set; }
    public SecurityFinding? SecurityFinding { get; set; }
    public Guid? PullRequestId { get; set; }
    public PullRequest? PullRequest { get; set; }
    public string StatusCode { get; set; } = default!;
    public int AttemptCount { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
