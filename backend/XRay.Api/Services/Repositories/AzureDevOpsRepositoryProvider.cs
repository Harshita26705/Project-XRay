using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace XRay.Api.Services.Repositories;

/// <summary>
/// Real Azure DevOps Git REST API calls authenticated with a Personal Access Token (PAT) rather
/// than full delegated OAuth — see documents/setup-guide.md for why: it avoids requiring a second
/// Azure AD app-registration/consent flow beyond the one already used for sign-in, while still
/// being genuine, working Azure DevOps integration rather than a simulated connection.
/// </summary>
public class AzureDevOpsRepositoryProvider : IRepositoryProvider
{
    private const string ApiVersion = "7.1";
    private readonly IHttpClientFactory _httpClientFactory;

    public AzureDevOpsRepositoryProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string ProviderCode => "AZURE_DEVOPS";

    public async Task<RepositoryValidationResult> ValidateAsync(RepositoryConnectionInfo connection, CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient(connection);
            var url = $"{Trim(connection.OrganizationUrl)}/{connection.ProjectName}/_apis/git/repositories/{connection.RepositoryName}?api-version={ApiVersion}";
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                return new RepositoryValidationResult(false, $"Azure DevOps returned {(int)response.StatusCode}: {response.ReasonPhrase}");
            }
            return new RepositoryValidationResult(true, null);
        }
        catch (Exception ex)
        {
            return new RepositoryValidationResult(false, ex.Message);
        }
    }

    public async Task<IReadOnlyList<BranchInfo>> ListBranchesAsync(RepositoryConnectionInfo connection, CancellationToken ct = default)
    {
        using var client = CreateClient(connection);
        var url = $"{Trim(connection.OrganizationUrl)}/{connection.ProjectName}/_apis/git/repositories/{connection.RepositoryName}/refs?filter=heads&api-version={ApiVersion}";
        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var result = new List<BranchInfo>();
        foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
        {
            var name = item.GetProperty("name").GetString()!.Replace("refs/heads/", "");
            var sha = item.GetProperty("objectId").GetString()!;
            result.Add(new BranchInfo(name, sha, name is "main" or "master"));
        }
        return result;
    }

    public async Task<string> GetHeadCommitAsync(RepositoryConnectionInfo connection, string branchName, CancellationToken ct = default)
    {
        var branches = await ListBranchesAsync(connection, ct);
        return branches.FirstOrDefault(b => b.Name == branchName)?.HeadCommitSha ?? "";
    }

    public async Task<IReadOnlyList<RepositoryFile>> ListFilesAsync(RepositoryConnectionInfo connection, string branchName, CancellationToken ct = default)
    {
        using var client = CreateClient(connection);
        var url = $"{Trim(connection.OrganizationUrl)}/{connection.ProjectName}/_apis/git/repositories/{connection.RepositoryName}/items" +
                  $"?scopePath=/&recursionLevel=Full&versionDescriptor.version={Uri.EscapeDataString(branchName)}&versionDescriptor.versionType=branch&api-version={ApiVersion}";
        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var allowedExtensions = new[] { ".cs", ".ts", ".tsx", ".sql" };
        var result = new List<RepositoryFile>();
        foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
        {
            if (item.TryGetProperty("isFolder", out var isFolder) && isFolder.GetBoolean()) continue;
            var path = item.GetProperty("path").GetString()!.TrimStart('/');
            if (!allowedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) continue;
            result.Add(new RepositoryFile(path, 0));
        }
        return result;
    }

    public async Task<string> GetFileContentAsync(RepositoryConnectionInfo connection, string branchName, string relativePath, CancellationToken ct = default)
    {
        using var client = CreateClient(connection);
        var url = $"{Trim(connection.OrganizationUrl)}/{connection.ProjectName}/_apis/git/repositories/{connection.RepositoryName}/items" +
                  $"?path={Uri.EscapeDataString(relativePath)}&versionDescriptor.version={Uri.EscapeDataString(branchName)}&versionDescriptor.versionType=branch" +
                  $"&includeContent=true&api-version={ApiVersion}";
        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private HttpClient CreateClient(RepositoryConnectionInfo connection)
    {
        if (string.IsNullOrWhiteSpace(connection.PersonalAccessToken))
        {
            throw new InvalidOperationException("Azure DevOps connection requires a Personal Access Token.");
        }

        var client = _httpClientFactory.CreateClient();
        var basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{connection.PersonalAccessToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
        return client;
    }

    private static string Trim(string? organizationUrl) => (organizationUrl ?? "").TrimEnd('/');
}
