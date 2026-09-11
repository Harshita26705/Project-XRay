namespace XRay.Domain.Identity;

public class Organization
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class UserAccount
{
    public Guid UserId { get; set; }
    public string ExternalIdentityId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class OrganizationMembership
{
    public Guid OrganizationMembershipId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public Guid UserId { get; set; }
    public UserAccount? User { get; set; }
    public string RoleCode { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
