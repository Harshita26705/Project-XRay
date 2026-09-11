namespace XRay.Api.Services.Repositories;

/// <summary>
/// Where a repository's source lives and how to authenticate to it. Local repositories only need
/// RootPath; Azure DevOps repositories need the REST API coordinates + a PAT resolved server-side
/// (never sent to or accepted from the frontend).
/// </summary>
public record RepositoryConnectionInfo(
    string ProviderCode,
    string? RootPath = null,
    string? OrganizationUrl = null,
    string? ProjectName = null,
    string? RepositoryName = null,
    string? PersonalAccessToken = null);

public record BranchInfo(string Name, string HeadCommitSha, bool IsDefault);

public record RepositoryFile(string RelativePath, long SizeBytes);

public record RepositoryValidationResult(bool IsValid, string? ErrorMessage);

/// <summary>
/// Abstraction over "where does the source code for this repository/branch come from" so the
/// ingestion pipeline (parsers, graph builder) never needs provider-specific logic — see
/// Figma/update.md Workstream A.
/// </summary>
public interface IRepositoryProvider
{
    string ProviderCode { get; }

    Task<RepositoryValidationResult> ValidateAsync(RepositoryConnectionInfo connection, CancellationToken ct = default);

    Task<IReadOnlyList<BranchInfo>> ListBranchesAsync(RepositoryConnectionInfo connection, CancellationToken ct = default);

    Task<string> GetHeadCommitAsync(RepositoryConnectionInfo connection, string branchName, CancellationToken ct = default);

    Task<IReadOnlyList<RepositoryFile>> ListFilesAsync(RepositoryConnectionInfo connection, string branchName, CancellationToken ct = default);

    Task<string> GetFileContentAsync(RepositoryConnectionInfo connection, string branchName, string relativePath, CancellationToken ct = default);
}
