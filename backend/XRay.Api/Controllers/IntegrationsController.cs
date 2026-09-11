using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XRay.Api.Contracts;
using XRay.Api.Services;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly IntegrationService _integrations;
    private readonly ICurrentUserService _currentUser;

    public IntegrationsController(IntegrationService integrations, ICurrentUserService currentUser)
    {
        _integrations = integrations;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IntegrationConnectionResponse>>> List(CancellationToken ct) =>
        Ok(await _integrations.ListAsync(await ResolveOrgAsync(ct), ct));

    [HttpPost]
    public async Task<ActionResult<IntegrationConnectionResponse>> Upsert(CreateIntegrationRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _integrations.CreateOrUpdateAsync(await ResolveOrgAsync(ct), request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{connectionId:guid}/test")]
    public async Task<ActionResult<IntegrationConnectionResponse>> Test(Guid connectionId, CancellationToken ct)
    {
        try
        {
            return Ok(await _integrations.TestConnectionAsync(connectionId, ct));
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
