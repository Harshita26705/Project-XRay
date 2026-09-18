using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

public sealed record AzureDevOpsRepositoryResponse(string Id, string Name, string? WebUrl, string? DefaultBranch);
public sealed record AzureDevOpsBranchResponse(string Name, string? ObjectId, bool IsDefault);
public sealed record AzureDevOpsWorkItemResponse(int Id, string Title, string? WorkItemType, string? State, string? AssignedTo, string? Description = null);
public sealed record AzureDevOpsPullRequestResponse(int Id, string Title, string? Description, string? Status, string? CreatedBy, string? SourceBranch, string? TargetBranch, IReadOnlyList<string> ChangedFilePaths);

public class AzureDevOpsService
{
    private const string ApiVersion = "7.1";
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public AzureDevOpsService(AppDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<AzureDevOpsRepositoryResponse>> ListRepositoriesAsync(Guid organizationId, CancellationToken ct)
    {
        var connection = await GetConnectionAsync(organizationId, ct);
        var baseUrl = RequireBaseUrl(connection.ExternalBaseUrl);
        using var response = await SendAsync(HttpMethod.Get, $"{baseUrl}/_apis/git/repositories?api-version={ApiVersion}", connection.PersonalAccessToken, ct);
        await EnsureSuccessAsync(response, "Unable to list Azure DevOps repositories.", ct);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return document.RootElement.GetProperty("value").EnumerateArray().Select(repository => new AzureDevOpsRepositoryResponse(
            repository.GetProperty("id").GetString()!,
            repository.GetProperty("name").GetString()!,
            repository.TryGetProperty("webUrl", out var webUrl) ? webUrl.GetString() : null,
            repository.TryGetProperty("defaultBranch", out var defaultBranch) ? defaultBranch.GetString() : null)).ToList();
    }

    public async Task<IReadOnlyList<AzureDevOpsBranchResponse>> ListBranchesAsync(Guid organizationId, string repositoryId, CancellationToken ct)
    {
        var connection = await GetConnectionAsync(organizationId, ct);
        var baseUrl = RequireBaseUrl(connection.ExternalBaseUrl);
        using var response = await SendAsync(HttpMethod.Get,
            $"{baseUrl}/_apis/git/repositories/{Uri.EscapeDataString(repositoryId)}/refs?filter=heads/&api-version={ApiVersion}", connection.PersonalAccessToken, ct);
        await EnsureSuccessAsync(response, "Unable to list Azure DevOps branches.", ct);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return document.RootElement.GetProperty("value").EnumerateArray().Select(branch =>
        {
            var fullName = branch.GetProperty("name").GetString() ?? string.Empty;
            var name = fullName.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase)
                ? fullName["refs/heads/".Length..]
                : fullName;
            return new AzureDevOpsBranchResponse(name,
                branch.TryGetProperty("objectId", out var objectId) ? objectId.GetString() : null,
                false);
        }).OrderBy(branch => branch.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<AzureDevOpsWorkItemResponse>> ListWorkItemsAsync(Guid organizationId, int top, CancellationToken ct)
    {
        var connection = await GetConnectionAsync(organizationId, ct);
        var baseUrl = RequireBaseUrl(connection.ExternalBaseUrl);

        // WIQL at the organization level can't resolve an Area Path scope and fails with
        // "[Area Path] under ''" — Azure DevOps requires WIQL to run against a specific project.
        var projectNames = await ListProjectNamesAsync(baseUrl, connection.PersonalAccessToken, ct);
        if (projectNames.Count == 0) return Array.Empty<AzureDevOpsWorkItemResponse>();

        var wiql = new { query = "SELECT [System.Id] FROM WorkItems ORDER BY [System.ChangedDate] DESC" };
        var ids = new List<int>();
        foreach (var projectName in projectNames.Take(5))
        {
            if (ids.Count >= top) break;
            using var wiqlRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{baseUrl}/{Uri.EscapeDataString(projectName)}/_apis/wit/wiql?api-version={ApiVersion}")
            {
                Content = JsonContent.Create(wiql)
            };
            ApplyAuth(wiqlRequest, connection.PersonalAccessToken);
            using var wiqlResponse = await _httpClientFactory.CreateClient().SendAsync(wiqlRequest, ct);
            if (!wiqlResponse.IsSuccessStatusCode) continue; // best-effort: skip projects without WIT access

            using var wiqlDocument = await JsonDocument.ParseAsync(await wiqlResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (wiqlDocument.RootElement.TryGetProperty("workItems", out var workItemsElement))
            {
                ids.AddRange(workItemsElement.EnumerateArray().Select(w => w.GetProperty("id").GetInt32()));
            }
        }

        ids = ids.Distinct().Take(top).ToList();
        if (ids.Count == 0) return Array.Empty<AzureDevOpsWorkItemResponse>();

        var idsParam = string.Join(',', ids);
        using var detailsResponse = await SendAsync(HttpMethod.Get,
            $"{baseUrl}/_apis/wit/workitems?ids={idsParam}&api-version={ApiVersion}", connection.PersonalAccessToken, ct);
        await EnsureSuccessAsync(detailsResponse, "Unable to load Azure DevOps work item details.", ct);

        using var detailsDocument = await JsonDocument.ParseAsync(await detailsResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return detailsDocument.RootElement.GetProperty("value").EnumerateArray().Select(item =>
        {
            var fields = item.GetProperty("fields");
            return new AzureDevOpsWorkItemResponse(
                item.GetProperty("id").GetInt32(),
                fields.TryGetProperty("System.Title", out var title) ? title.GetString() ?? "" : "",
                fields.TryGetProperty("System.WorkItemType", out var type) ? type.GetString() : null,
                fields.TryGetProperty("System.State", out var state) ? state.GetString() : null,
                fields.TryGetProperty("System.AssignedTo", out var assignedTo) && assignedTo.TryGetProperty("displayName", out var displayName) ? displayName.GetString() : null);
        }).ToList();
    }

    private async Task<List<string>> ListProjectNamesAsync(string baseUrl, string? personalAccessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/_apis/projects?api-version={ApiVersion}");
        ApplyAuth(request, personalAccessToken);
        using var response = await _httpClientFactory.CreateClient().SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return new List<string>();

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (!document.RootElement.TryGetProperty("value", out var valueElement)) return new List<string>();
        return valueElement.EnumerateArray().Select(p => p.GetProperty("name").GetString()!).ToList();
    }

    /// <summary>Fetches one work item's full details (title, description, type, state, assignee) for auto-filling the Analyze-a-Change form.</summary>
    public async Task<AzureDevOpsWorkItemResponse> GetWorkItemAsync(Guid organizationId, int workItemId, CancellationToken ct)
    {
        var connection = await GetConnectionAsync(organizationId, ct);
        var baseUrl = RequireBaseUrl(connection.ExternalBaseUrl);
        using var response = await SendAsync(HttpMethod.Get,
            $"{baseUrl}/_apis/wit/workitems/{workItemId}?$expand=fields&api-version={ApiVersion}", connection.PersonalAccessToken, ct);
        await EnsureSuccessAsync(response, $"Unable to fetch Azure DevOps work item {workItemId}.", ct);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var fields = document.RootElement.GetProperty("fields");
        return new AzureDevOpsWorkItemResponse(
            document.RootElement.GetProperty("id").GetInt32(),
            fields.TryGetProperty("System.Title", out var title) ? title.GetString() ?? "" : "",
            fields.TryGetProperty("System.WorkItemType", out var type) ? type.GetString() : null,
            fields.TryGetProperty("System.State", out var state) ? state.GetString() : null,
            fields.TryGetProperty("System.AssignedTo", out var assignedTo) && assignedTo.TryGetProperty("displayName", out var displayName) ? displayName.GetString() : null,
            fields.TryGetProperty("System.Description", out var description) ? StripHtml(description.GetString()) : null);
    }

    /// <summary>Work item descriptions are stored as HTML — strip tags so the plain-text field in the Analyze-a-Change form reads cleanly.</summary>
    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
    }

    /// <summary>Fetches one pull request's title, description, status, and changed file paths for auto-filling the Analyze-a-Change form. PR IDs are unique org-wide, so no project/repo needs to be known up front.</summary>
    public async Task<AzureDevOpsPullRequestResponse> GetPullRequestAsync(Guid organizationId, int pullRequestId, CancellationToken ct)
    {
        var connection = await GetConnectionAsync(organizationId, ct);
        var baseUrl = RequireBaseUrl(connection.ExternalBaseUrl);
        using var response = await SendAsync(HttpMethod.Get,
            $"{baseUrl}/_apis/git/pullrequests/{pullRequestId}?api-version={ApiVersion}", connection.PersonalAccessToken, ct);
        await EnsureSuccessAsync(response, $"Unable to fetch Azure DevOps pull request {pullRequestId}.", ct);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = document.RootElement;
        var repositoryId = root.GetProperty("repository").GetProperty("id").GetString()!;

        var changedFilePaths = await TryListChangedFilePathsAsync(baseUrl, repositoryId, pullRequestId, connection.PersonalAccessToken, ct);

        return new AzureDevOpsPullRequestResponse(
            root.GetProperty("pullRequestId").GetInt32(),
            root.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
            root.TryGetProperty("description", out var description) ? description.GetString() : null,
            root.TryGetProperty("status", out var status) ? status.GetString() : null,
            root.TryGetProperty("createdBy", out var createdBy) && createdBy.TryGetProperty("displayName", out var displayName) ? displayName.GetString() : null,
            root.TryGetProperty("sourceRefName", out var sourceRef) ? StripRefPrefix(sourceRef.GetString()) : null,
            root.TryGetProperty("targetRefName", out var targetRef) ? StripRefPrefix(targetRef.GetString()) : null,
            changedFilePaths);
    }

    /// <summary>Best-effort: if the iteration/changes calls fail for any reason, the PR's title/description are still useful on their own.</summary>
    private async Task<IReadOnlyList<string>> TryListChangedFilePathsAsync(string baseUrl, string repositoryId, int pullRequestId, string? personalAccessToken, CancellationToken ct)
    {
        try
        {
            using var iterationsResponse = await SendAsync(HttpMethod.Get,
                $"{baseUrl}/_apis/git/repositories/{repositoryId}/pullrequests/{pullRequestId}/iterations?api-version={ApiVersion}", personalAccessToken, ct);
            if (!iterationsResponse.IsSuccessStatusCode) return Array.Empty<string>();

            using var iterationsDocument = await JsonDocument.ParseAsync(await iterationsResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!iterationsDocument.RootElement.TryGetProperty("value", out var iterationsValue) || iterationsValue.GetArrayLength() == 0)
            {
                return Array.Empty<string>();
            }
            var latestIterationId = iterationsValue.EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).Max();

            using var changesResponse = await SendAsync(HttpMethod.Get,
                $"{baseUrl}/_apis/git/repositories/{repositoryId}/pullrequests/{pullRequestId}/iterations/{latestIterationId}/changes?api-version={ApiVersion}", personalAccessToken, ct);
            if (!changesResponse.IsSuccessStatusCode) return Array.Empty<string>();

            using var changesDocument = await JsonDocument.ParseAsync(await changesResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!changesDocument.RootElement.TryGetProperty("changeEntries", out var changeEntries)) return Array.Empty<string>();

            return changeEntries.EnumerateArray()
                .Select(entry => entry.TryGetProperty("item", out var item) && item.TryGetProperty("path", out var path) ? path.GetString() : null)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path!.TrimStart('/'))
                .Distinct()
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static string? StripRefPrefix(string? refName) =>
        refName?.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase) == true ? refName["refs/heads/".Length..] : refName;

    private async Task<Domain.Integrations.IntegrationConnection> GetConnectionAsync(Guid organizationId, CancellationToken ct) =>
        await _db.IntegrationConnections
            .FirstOrDefaultAsync(connection => connection.OrganizationId == organizationId &&
                                               connection.IntegrationProviderId == _db.IntegrationProviders
                                                   .Where(provider => provider.Code == "AZURE_DEVOPS")
                                                   .Select(provider => provider.Id)
                                                   .FirstOrDefault() &&
                                               connection.IsEnabled, ct)
        ?? throw new InvalidOperationException("Azure DevOps is not configured for this organization.");

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? personalAccessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url);
        ApplyAuth(request, personalAccessToken);
        return await _httpClientFactory.CreateClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    /// <summary>Azure DevOps requires a Basic-auth PAT, never the caller's own sign-in token — forwarding that token here would never authenticate against dev.azure.com.</summary>
    private static void ApplyAuth(HttpRequestMessage request, string? personalAccessToken)
    {
        if (string.IsNullOrWhiteSpace(personalAccessToken))
        {
            throw new InvalidOperationException("Configure a Personal Access Token for the Azure DevOps integration before using it.");
        }
        var basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{personalAccessToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
    }

    private static string RequireBaseUrl(string? baseUrl) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.ToString().TrimEnd('/')
            : throw new InvalidOperationException("Configure the Azure DevOps organization URL before discovering repositories.");

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string message, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var detail = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException($"{message} ({(int)response.StatusCode}): {detail[..Math.Min(detail.Length, 500)]}");
    }
}
