using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Integrations;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

/// <summary>
/// Manages IntegrationConnection records. "Test Connection" performs a real HTTP reachability probe
/// against the connection's configured base URL for every provider (Azure DevOps, Microsoft Foundry,
/// Azure AI Search, Microsoft Teams, Power Automate). This confirms the endpoint is reachable; it
/// does not perform provider-specific authenticated calls.
/// </summary>
public class IntegrationService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public IntegrationService(AppDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<IntegrationConnectionResponse>> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        var providers = await _db.IntegrationProviders.ToDictionaryAsync(p => p.Id, p => p.Code, ct);
        var connections = await _db.IntegrationConnections.Where(c => c.OrganizationId == organizationId).ToListAsync(ct);
        return connections.Select(c => new IntegrationConnectionResponse(
            c.IntegrationConnectionId, providers.GetValueOrDefault(c.IntegrationProviderId, "UNKNOWN"), c.DisplayName, c.StatusCode, c.LastTestedAtUtc, c.IsEnabled, c.ExternalBaseUrl, !string.IsNullOrWhiteSpace(c.PersonalAccessToken))).ToList();
    }

    public async Task<IntegrationConnectionResponse> CreateOrUpdateAsync(Guid organizationId, CreateIntegrationRequest request, CancellationToken ct = default)
    {
        var providerId = await _db.IntegrationProviders.Where(p => p.Code == request.Provider).Select(p => p.Id).FirstOrDefaultAsync(ct);
        if (providerId == 0) throw new InvalidOperationException($"Unknown integration provider '{request.Provider}'.");

        var connection = await _db.IntegrationConnections.FirstOrDefaultAsync(
            c => c.OrganizationId == organizationId && c.IntegrationProviderId == providerId, ct);

        if (connection is null)
        {
            connection = new IntegrationConnection
            {
                IntegrationConnectionId = Guid.NewGuid(),
                OrganizationId = organizationId,
                IntegrationProviderId = providerId,
                DisplayName = request.DisplayName,
                StatusCode = "CONFIGURED",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            _db.IntegrationConnections.Add(connection);
        }

        connection.ExternalBaseUrl = request.ExternalBaseUrl;
        connection.ExternalTenantId = request.ExternalTenantId;
        // Write-only: only overwrite when a new value is actually submitted, so re-saving the
        // endpoint URL doesn't blank out a PAT the caller didn't intend to change.
        if (!string.IsNullOrWhiteSpace(request.PersonalAccessToken))
        {
            connection.PersonalAccessToken = request.PersonalAccessToken;
        }
        connection.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return new IntegrationConnectionResponse(connection.IntegrationConnectionId, request.Provider, connection.DisplayName, connection.StatusCode, connection.LastTestedAtUtc, connection.IsEnabled, connection.ExternalBaseUrl, !string.IsNullOrWhiteSpace(connection.PersonalAccessToken));
    }

    public async Task<IntegrationConnectionResponse> TestConnectionAsync(Guid connectionId, CancellationToken ct = default)
    {
        var connection = await _db.IntegrationConnections.FirstOrDefaultAsync(c => c.IntegrationConnectionId == connectionId, ct)
                          ?? throw new InvalidOperationException("Integration connection not found.");
        var providerCode = await _db.IntegrationProviders.Where(p => p.Id == connection.IntegrationProviderId).Select(p => p.Code).FirstAsync(ct);

        var ok = providerCode switch
        {
            "AZURE_DEVOPS" => await ProbeHttpEndpointAsync(connection.ExternalBaseUrl, ct, connection.PersonalAccessToken),
            "MICROSOFT_FOUNDRY" or "AZURE_AI_SEARCH" or "MICROSOFT_TEAMS" or "POWER_AUTOMATE"
                => await ProbeHttpEndpointAsync(connection.ExternalBaseUrl, ct),
            _ => false,
        };

        connection.StatusCode = ok ? "CONNECTED" : "ERROR";
        connection.LastTestedAtUtc = DateTime.UtcNow;
        connection.LastError = ok ? null : "Could not reach the configured endpoint.";
        await _db.SaveChangesAsync(ct);

        return new IntegrationConnectionResponse(connection.IntegrationConnectionId, providerCode, connection.DisplayName, connection.StatusCode, connection.LastTestedAtUtc, connection.IsEnabled, connection.ExternalBaseUrl, !string.IsNullOrWhiteSpace(connection.PersonalAccessToken));
    }

    private async Task<bool> ProbeHttpEndpointAsync(string? baseUrl, CancellationToken ct, string? personalAccessToken = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return false;
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl);
            if (!string.IsNullOrWhiteSpace(personalAccessToken))
            {
                var basicAuth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);
            }
            var response = await client.SendAsync(request, ct);
            return (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }
}
