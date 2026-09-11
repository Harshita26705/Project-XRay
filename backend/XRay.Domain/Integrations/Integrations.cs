using XRay.Domain.Identity;
using XRay.Domain.Projects;

namespace XRay.Domain.Integrations;

public class SecretReference
{
    public Guid SecretReferenceId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public string ProviderCode { get; set; } = default!;
    public string SecretUri { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}

public class IntegrationConnection
{
    public Guid IntegrationConnectionId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public byte IntegrationProviderId { get; set; }
    public string DisplayName { get; set; } = default!;
    public string? ExternalTenantId { get; set; }
    public string? ExternalBaseUrl { get; set; }
    public Guid? SecretReferenceId { get; set; }
    public SecretReference? SecretReference { get; set; }
    public string StatusCode { get; set; } = default!;
    public DateTime? LastTestedAtUtc { get; set; }
    public string? LastError { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
