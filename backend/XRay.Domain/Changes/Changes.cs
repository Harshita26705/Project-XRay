using XRay.Domain.Identity;
using XRay.Domain.Ingestion;
using XRay.Domain.Projects;

namespace XRay.Domain.Changes;

public class Change
{
    public Guid ChangeId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public byte ChangeTypeId { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? ExternalChangeId { get; set; }
    public string? ExternalUrl { get; set; }
    public Guid? AuthorUserId { get; set; }
    public UserAccount? AuthorUser { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class PullRequest
{
    public Guid PullRequestId { get; set; }
    public Guid ChangeId { get; set; }
    public Change? Change { get; set; }
    public Guid RepositoryId { get; set; }
    public Repository? Repository { get; set; }
    public string ProviderPullRequestId { get; set; } = default!;
    public string? SourceBranchName { get; set; }
    public string? TargetBranchName { get; set; }
    public string? ProviderStatusCode { get; set; }
    public int? FilesChangedCount { get; set; }
    public int? LinesAdded { get; set; }
    public int? LinesDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class WorkItem
{
    public Guid WorkItemId { get; set; }
    public Guid ChangeId { get; set; }
    public Change? Change { get; set; }
    public string ExternalWorkItemId { get; set; } = default!;
    public string? Priority { get; set; }
    public string? AssignedTo { get; set; }
    public string? State { get; set; }
    public DateTime? ProviderUpdatedAtUtc { get; set; }
}

public class Commit
{
    public Guid CommitId { get; set; }
    public Guid RepositoryId { get; set; }
    public Repository? Repository { get; set; }
    public Guid? ChangeId { get; set; }
    public Change? Change { get; set; }
    public string CommitHash { get; set; } = default!;
    public string? ParentHash { get; set; }
    public string? Message { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public DateTime? CommittedAtUtc { get; set; }
}

public class ChangeFile
{
    public Guid ChangeFileId { get; set; }
    public Guid ChangeId { get; set; }
    public Change? Change { get; set; }
    public Guid? CodeFileId { get; set; }
    public CodeFile? CodeFile { get; set; }
    public string RelativePath { get; set; } = default!;
    public string ChangeKindCode { get; set; } = default!;
    public string? OldPath { get; set; }
    public string? NewPath { get; set; }
    public int? LinesAdded { get; set; }
    public int? LinesDeleted { get; set; }
}
