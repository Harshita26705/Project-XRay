using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XRay.Api.Services;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;

    public MeController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await _currentUser.GetOrProvisionUserAsync(User, ct);
        var org = await _currentUser.GetOrProvisionOrganizationAsync(user, ct);
        return Ok(new { user.UserId, user.DisplayName, user.Email, OrganizationId = org.OrganizationId, OrganizationName = org.Name });
    }
}
