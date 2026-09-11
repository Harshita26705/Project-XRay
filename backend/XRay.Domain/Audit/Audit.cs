using XRay.Domain.Identity;

namespace XRay.Domain.Audit;

public class AuditEvent
{
    public long AuditEventId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid? UserId { get; set; }
    public UserAccount? User { get; set; }
    public string EntityTypeCode { get; set; } = default!;
    public Guid? EntityId { get; set; }
    public string ActionCode { get; set; } = default!;
    public Guid? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
