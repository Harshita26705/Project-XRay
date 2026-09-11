using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XRay.Api.Contracts;
using XRay.Api.Services;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notifications;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(NotificationService notifications, ICurrentUserService currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    [HttpGet("channels")]
    public async Task<ActionResult<IReadOnlyList<NotificationChannelResponse>>> Channels(CancellationToken ct) =>
        Ok(await _notifications.ListChannelsAsync(await ResolveOrgAsync(ct), ct));

    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<NotificationRuleResponse>>> Rules(CancellationToken ct) =>
        Ok(await _notifications.ListRulesAsync(await ResolveOrgAsync(ct), ct));

    [HttpPut("rules/{ruleId:guid}")]
    public async Task<ActionResult<NotificationRuleResponse>> UpdateRule(Guid ruleId, UpdateNotificationRuleRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _notifications.UpdateRuleAsync(ruleId, request.IsEnabled, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private async Task<Guid> ResolveOrgAsync(CancellationToken ct)
    {
        var user = await _currentUser.GetOrProvisionUserAsync(User, ct);
        var org = await _currentUser.GetOrProvisionOrganizationAsync(user, ct);
        return org.OrganizationId;
    }
}
