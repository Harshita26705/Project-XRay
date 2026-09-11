using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XRay.Api.Contracts;
using XRay.Api.Services;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly SettingsService _settings;
    private readonly ICurrentUserService _currentUser;

    public SettingsController(SettingsService settings, ICurrentUserService currentUser)
    {
        _settings = settings;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<OrganizationSettingsResponse>> Get(CancellationToken ct)
    {
        var user = await _currentUser.GetOrProvisionUserAsync(User, ct);
        var org = await _currentUser.GetOrProvisionOrganizationAsync(user, ct);
        return Ok(await _settings.GetAsync(org.OrganizationId, ct));
    }

    [HttpPut]
    public async Task<ActionResult<OrganizationSettingsResponse>> Update(UpdateOrganizationSettingsRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetOrProvisionUserAsync(User, ct);
        var org = await _currentUser.GetOrProvisionOrganizationAsync(user, ct);
        return Ok(await _settings.UpdateAsync(org.OrganizationId, user.UserId, request, ct));
    }
}
