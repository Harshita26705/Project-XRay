using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XRay.Api.Services;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/azure-devops")]
public class AzureDevOpsController : ControllerBase
{
    private readonly AzureDevOpsService _azureDevOps;
    private readonly ICurrentUserService _currentUser;

    public AzureDevOpsController(AzureDevOpsService azureDevOps, ICurrentUserService currentUser)
    {
        _azureDevOps = azureDevOps;
        _currentUser = currentUser;
    }

    [HttpGet("repositories")]
    public async Task<ActionResult<IReadOnlyList<AzureDevOpsRepositoryResponse>>> Repositories(CancellationToken ct)
    {
        try
        {
            var organization = await ResolveOrganizationAsync(ct);
            return Ok(await _azureDevOps.ListRepositoriesAsync(organization, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("repositories/{repositoryId}/branches")]
    public async Task<ActionResult<IReadOnlyList<AzureDevOpsBranchResponse>>> Branches(string repositoryId, CancellationToken ct)
    {
        try
        {
            var organization = await ResolveOrganizationAsync(ct);
            return Ok(await _azureDevOps.ListBranchesAsync(organization, repositoryId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<Guid> ResolveOrganizationAsync(CancellationToken ct)
    {
        var user = await _currentUser.GetOrProvisionUserAsync(User, ct);
        var organization = await _currentUser.GetOrProvisionOrganizationAsync(user, ct);
        return organization.OrganizationId;
    }
}
