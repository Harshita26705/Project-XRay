using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XRay.Api.Contracts;
using XRay.Api.Services;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class ChangesController : ControllerBase
{
    private readonly ChangeService _changes;
    private readonly ICurrentUserService _currentUser;

    public ChangesController(ChangeService changes, ICurrentUserService currentUser)
    {
        _changes = changes;
        _currentUser = currentUser;
    }

    [HttpGet("projects/{projectId:guid}/changes")]
    public async Task<ActionResult<IReadOnlyList<ChangeResponse>>> List(Guid projectId, CancellationToken ct) =>
        Ok(await _changes.ListAsync(projectId, ct));

    [HttpGet("changes/{changeId:guid}")]
    public async Task<ActionResult<ChangeResponse>> Get(Guid changeId, CancellationToken ct)
    {
        var change = await _changes.GetAsync(changeId, ct);
        return change is null ? NotFound() : Ok(change);
    }

    [HttpPost("projects/{projectId:guid}/changes")]
    public async Task<ActionResult<ChangeResponse>> Create(Guid projectId, CreateChangeRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetOrProvisionUserAsync(User, ct);
        var org = await _currentUser.GetOrProvisionOrganizationAsync(user, ct);
        var created = await _changes.CreateAsync(org.OrganizationId, projectId, user.UserId, request, ct);
        return CreatedAtAction(nameof(Get), new { changeId = created.ChangeId }, created);
    }
}
