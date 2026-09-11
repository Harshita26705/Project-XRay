using Microsoft.EntityFrameworkCore;
using XRay.Domain.Identity;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

public interface ICurrentUserService
{
    Task<UserAccount> GetOrProvisionUserAsync(System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct = default);
    Task<Organization> GetOrProvisionOrganizationAsync(UserAccount user, CancellationToken ct = default);
}

/// <summary>
/// Just-in-time provisioning: the first time an authenticated Microsoft Entra ID user is seen,
/// create their UserAccount + a personal Organization + membership. Real deployments would likely
/// separate org creation into an explicit onboarding flow, but JIT keeps the demo frictionless.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly AppDbContext _db;

    public CurrentUserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserAccount> GetOrProvisionUserAsync(System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var externalId = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                          ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? "dev-local-user";
        var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("preferred_username")?.Value
                    ?? "dev@localhostnew1";
        var name = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? email;

        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.ExternalIdentityId == externalId, ct);
        if (user is not null)
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return user;
        }

        user = new UserAccount
        {
            UserId = Guid.NewGuid(),
            ExternalIdentityId = externalId,
            Email = email,
            DisplayName = name,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            LastLoginAtUtc = DateTime.UtcNow,
        };
        _db.UserAccounts.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<Organization> GetOrProvisionOrganizationAsync(UserAccount user, CancellationToken ct = default)
    {
        var membership = await _db.OrganizationMemberships
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == user.UserId, ct);

        if (membership?.Organization is not null) return membership.Organization;

        var org = new Organization
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Project X-Ray Enterprise",
            Slug = $"neworg-{user.UserId:N}"[..20],
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        _db.Organizations.Add(org);

        _db.OrganizationMemberships.Add(new OrganizationMembership
        {
            OrganizationMembershipId = Guid.NewGuid(),
            OrganizationId = org.OrganizationId,
            UserId = user.UserId,
            RoleCode = "OWNER",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        return org;
    }
}
