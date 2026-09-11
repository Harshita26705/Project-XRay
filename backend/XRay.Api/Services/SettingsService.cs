using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Settings;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

public class SettingsService
{
    private readonly AppDbContext _db;

    public SettingsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrganizationSettingsResponse> GetAsync(Guid organizationId, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstAsync(o => o.OrganizationId == organizationId, ct);
        var settings = await _db.OrganizationSettings.Where(s => s.OrganizationId == organizationId).ToDictionaryAsync(s => s.SettingKey, ct);

        return new OrganizationSettingsResponse(
            org.Name,
            settings.TryGetValue("DEFAULT_TARGET_PROJECT", out var proj) && Guid.TryParse(proj.StringValue, out var projId) ? projId : null,
            settings.TryGetValue("INCLUDE_SECURITY_SCAN", out var sec) ? sec.BoolValue ?? true : true,
            settings.TryGetValue("INCLUDE_AI_EXPLANATION", out var ai) ? ai.BoolValue ?? true : true,
            settings.TryGetValue("AUTO_ANALYZE_PULL_REQUESTS", out var auto) && (auto.BoolValue ?? false));
    }

    public async Task<OrganizationSettingsResponse> UpdateAsync(Guid organizationId, Guid updatedByUserId, UpdateOrganizationSettingsRequest request, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstAsync(o => o.OrganizationId == organizationId, ct);
        org.Name = request.OrganizationName;
        org.UpdatedAtUtc = DateTime.UtcNow;

        await UpsertAsync(organizationId, updatedByUserId, "DEFAULT_TARGET_PROJECT", stringValue: request.DefaultTargetProjectId?.ToString(), ct: ct);
        await UpsertAsync(organizationId, updatedByUserId, "INCLUDE_SECURITY_SCAN", boolValue: request.IncludeSecurityScan, ct: ct);
        await UpsertAsync(organizationId, updatedByUserId, "INCLUDE_AI_EXPLANATION", boolValue: request.IncludeAiExplanation, ct: ct);
        await UpsertAsync(organizationId, updatedByUserId, "AUTO_ANALYZE_PULL_REQUESTS", boolValue: request.AutoAnalyzePullRequests, ct: ct);

        await _db.SaveChangesAsync(ct);
        return await GetAsync(organizationId, ct);
    }

    private async Task UpsertAsync(Guid organizationId, Guid updatedByUserId, string key, string? stringValue = null, bool? boolValue = null, CancellationToken ct = default)
    {
        var setting = await _db.OrganizationSettings.FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.SettingKey == key, ct);
        if (setting is null)
        {
            setting = new OrganizationSetting { OrganizationSettingId = Guid.NewGuid(), OrganizationId = organizationId, SettingKey = key, UpdatedByUserId = updatedByUserId };
            _db.OrganizationSettings.Add(setting);
        }
        setting.StringValue = stringValue;
        setting.BoolValue = boolValue;
        setting.UpdatedByUserId = updatedByUserId;
        setting.UpdatedAtUtc = DateTime.UtcNow;
    }
}
