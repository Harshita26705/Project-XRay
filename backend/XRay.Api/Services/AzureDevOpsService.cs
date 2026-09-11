using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

public sealed record AzureDevOpsRepositoryResponse(string Id, string Name, string? WebUrl, string? DefaultBranch);
public sealed record AzureDevOpsBranchResponse(string Name, string? ObjectId, bool IsDefault);

public class AzureDevOpsService
{
    private const string ApiVersion = "7.1";
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AzureDevOpsService(AppDbContext db, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<AzureDevOpsRepositoryResponse>> ListRepositoriesAsync(Guid organizationId, CancellationToken ct)
    {
        var connection = await GetConnectionAsync(organizationId, ct);
        var baseUrl = RequireBaseUrl(connection.ExternalBaseUrl);
        using var response = await SendAsync(HttpMethod.Get, $"{baseUrl}/_apis/git/repositories?api-version={ApiVersion}", ct);
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
            $"{baseUrl}/_apis/git/repositories/{Uri.EscapeDataString(repositoryId)}/refs?filter=heads/&api-version={ApiVersion}", ct);
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

    private async Task<Domain.Integrations.IntegrationConnection> GetConnectionAsync(Guid organizationId, CancellationToken ct) =>
        await _db.IntegrationConnections
            .FirstOrDefaultAsync(connection => connection.OrganizationId == organizationId &&
                                               connection.IntegrationProviderId == _db.IntegrationProviders
                                                   .Where(provider => provider.Code == "AZURE_DEVOPS")
                                                   .Select(provider => provider.Id)
                                                   .FirstOrDefault() &&
                                               connection.IsEnabled, ct)
        ?? throw new InvalidOperationException("Azure DevOps is not configured for this organization.");

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url);
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization) && AuthenticationHeaderValue.TryParse(authorization, out var header))
        {
            request.Headers.Authorization = header;
        }

        return await _httpClientFactory.CreateClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
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
