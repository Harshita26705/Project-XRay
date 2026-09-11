using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Notifications;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

public class NotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<NotificationChannelResponse>> ListChannelsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var channels = await _db.NotificationChannels.Where(c => c.OrganizationId == organizationId).ToListAsync(ct);
        if (channels.Count == 0)
        {
            channels = new List<NotificationChannel>
            {
                new() { NotificationChannelId = Guid.NewGuid(), OrganizationId = organizationId, ChannelTypeCode = "TEAMS", DisplayName = "Microsoft Teams", CreatedAtUtc = DateTime.UtcNow },
                new() { NotificationChannelId = Guid.NewGuid(), OrganizationId = organizationId, ChannelTypeCode = "AZURE_DEVOPS", DisplayName = "Azure DevOps", CreatedAtUtc = DateTime.UtcNow },
                new() { NotificationChannelId = Guid.NewGuid(), OrganizationId = organizationId, ChannelTypeCode = "EMAIL", DisplayName = "Email", CreatedAtUtc = DateTime.UtcNow },
            };
            _db.NotificationChannels.AddRange(channels);
            await _db.SaveChangesAsync(ct);
        }
        return channels.Select(c => new NotificationChannelResponse(c.NotificationChannelId, c.ChannelTypeCode, c.DisplayName, c.IsEnabled)).ToList();
    }

    public async Task<IReadOnlyList<NotificationRuleResponse>> ListRulesAsync(Guid organizationId, CancellationToken ct = default)
    {
        var channels = await ListChannelsAsync(organizationId, ct);
        var eventTypes = await _db.NotificationEventTypes.ToListAsync(ct);
        var existing = await _db.NotificationRules.Where(r => r.OrganizationId == organizationId).ToListAsync(ct);

        var missing = new List<NotificationRule>();
        foreach (var eventType in eventTypes)
        {
            foreach (var channel in channels)
            {
                if (existing.Any(r => r.NotificationEventTypeId == eventType.Id && r.NotificationChannelId == channel.NotificationChannelId)) continue;
                missing.Add(new NotificationRule
                {
                    NotificationRuleId = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    NotificationEventTypeId = eventType.Id,
                    NotificationChannelId = channel.NotificationChannelId,
                    IsEnabled = eventType.Code is "HIGH_RISK_CHANGE" or "CRITICAL_SECURITY_FINDING",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                });
            }
        }
        if (missing.Count > 0)
        {
            _db.NotificationRules.AddRange(missing);
            await _db.SaveChangesAsync(ct);
            existing.AddRange(missing);
        }

        var eventTypeByid = eventTypes.ToDictionary(e => e.Id, e => e.Code);
        var channelById = channels.ToDictionary(c => c.NotificationChannelId, c => c.ChannelType);
        return existing.Select(r => new NotificationRuleResponse(r.NotificationRuleId, eventTypeByid.GetValueOrDefault(r.NotificationEventTypeId, "UNKNOWN"), r.NotificationChannelId, channelById.GetValueOrDefault(r.NotificationChannelId, "UNKNOWN"), r.IsEnabled)).ToList();
    }

    public async Task<NotificationRuleResponse> UpdateRuleAsync(Guid ruleId, bool isEnabled, CancellationToken ct = default)
    {
        var rule = await _db.NotificationRules.FirstOrDefaultAsync(r => r.NotificationRuleId == ruleId, ct)
                   ?? throw new InvalidOperationException("Notification rule not found.");
        rule.IsEnabled = isEnabled;
        rule.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var eventType = await _db.NotificationEventTypes.Where(e => e.Id == rule.NotificationEventTypeId).Select(e => e.Code).FirstAsync(ct);
        var channelType = await _db.NotificationChannels.Where(c => c.NotificationChannelId == rule.NotificationChannelId).Select(c => c.ChannelTypeCode).FirstAsync(ct);
        return new NotificationRuleResponse(rule.NotificationRuleId, eventType, rule.NotificationChannelId, channelType, rule.IsEnabled);
    }
}
