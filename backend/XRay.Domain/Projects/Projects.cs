using XRay.Domain.Identity;

namespace XRay.Domain.Projects;

public class Project
{
    public Guid ProjectId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ExternalProjectId { get; set; }
    public string? ExternalProjectUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public UserAccount? CreatedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class ProjectMember
{
    public Guid ProjectMemberId { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid UserId { get; set; }
    public UserAccount? User { get; set; }
    public string RoleCode { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}

public class Repository
{
    public Guid RepositoryId { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public byte? IntegrationProviderId { get; set; }
    public string Name { get; set; } = default!;
    public string? ProviderRepositoryId { get; set; }
    public string? CloneUrl { get; set; }
    public string? WebUrl { get; set; }
    public string? DefaultBranchName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastIndexedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class Branch
{
    public Guid BranchId { get; set; }
    public Guid RepositoryId { get; set; }
    public Repository? Repository { get; set; }
    public string Name { get; set; } = default!;
    public string? ProviderBranchId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? LastIndexedCommitId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>Named "ProjectEnvironment" to avoid clashing with System.Environment.</summary>
public class ProjectEnvironment
{
    public Guid EnvironmentId { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
