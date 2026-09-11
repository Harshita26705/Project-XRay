using XRay.Domain.Identity;
using XRay.Domain.Projects;

namespace XRay.Domain.Jobs;

public class Job
{
    public Guid JobId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string JobTypeCode { get; set; } = default!;
    public string StatusCode { get; set; } = default!;
    public Guid? CorrelationId { get; set; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public DateTime QueuedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
