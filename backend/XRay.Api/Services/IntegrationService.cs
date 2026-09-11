using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Integrations;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

/// <summary>
/// Manages IntegrationConnection records. Azure DevOps and Microsoft Foundry get a real (best-effort)
/// connectivity check when configured; the remaining providers (Azure AI Search, Teams, Power
/// Automate) are modeled with connection state but "Test Connection" is simulated, per the plan's
/// documented integration-depth tradeoff.
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
            c.IntegrationConnectionId, providers.GetValueOrDefault(c.IntegrationProviderId, "UNKNOWN"), c.DisplayName, c.StatusCode, c.LastTestedAtUtc, c.IsEnabled)).ToList();
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
        connection.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return new IntegrationConnectionResponse(connection.IntegrationConnectionId, request.Provider, connection.DisplayName, connection.StatusCode, connection.LastTestedAtUtc, connection.IsEnabled);
    }

    public async Task<IntegrationConnectionResponse> TestConnectionAsync(Guid connectionId, CancellationToken ct = default)
    {
        var connection = await _db.IntegrationConnections.FirstOrDefaultAsync(c => c.IntegrationConnectionId == connectionId, ct)
                          ?? throw new InvalidOperationException("Integration connection not found.");
        var providerCode = await _db.IntegrationProviders.Where(p => p.Id == connection.IntegrationProviderId).Select(p => p.Code).FirstAsync(ct);

        var ok = providerCode switch
        {
            "AZURE_DEVOPS" or "MICROSOFT_FOUNDRY" => await ProbeHttpEndpointAsync(connection.ExternalBaseUrl, ct),
            _ => true, // simulated success for AI Search / Teams / Power Automate in this first pass
        };

        connection.StatusCode = ok ? "CONNECTED" : "ERROR";
        connection.LastTestedAtUtc = DateTime.UtcNow;
        connection.LastError = ok ? null : "Could not reach the configured endpoint.";
        await _db.SaveChangesAsync(ct);

        return new IntegrationConnectionResponse(connection.IntegrationConnectionId, providerCode, connection.DisplayName, connection.StatusCode, connection.LastTestedAtUtc, connection.IsEnabled);
    }

    private async Task<bool> ProbeHttpEndpointAsync(string? baseUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return false;
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync(baseUrl, ct);
            return (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }
}
