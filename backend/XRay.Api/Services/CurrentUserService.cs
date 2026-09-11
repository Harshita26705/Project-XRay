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
                    ?? "dev@localhost";
        var name = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? email;

        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.ExternalIdentityId == externalId, ct);
        if (user is not null)
        {
            // Best-effort only: concurrent requests racing on the same RowVersion must not fail the request.
            try
            {
                user.LastLoginAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.Entry(user).State = EntityState.Unchanged;
            }
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

        try
        {
            await _db.SaveChangesAsync(ct);
            return user;
        }
        catch (DbUpdateException)
        {
            // Idempotency under concurrent first-login races: another request already inserted the
            // same ExternalIdentityId (unique index) between our SELECT and INSERT. Detach the failed
            // insert and return the row that won the race instead of surfacing a 500 to the client.
            _db.Entry(user).State = EntityState.Detached;
            return await _db.UserAccounts.FirstAsync(u => u.ExternalIdentityId == externalId, ct);
        }
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
            Slug = $"org-{user.UserId:N}"[..20],
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

        try
        {
            await _db.SaveChangesAsync(ct);
            return org;
        }
        catch (DbUpdateException)
        {
            // Same idempotency guard as GetOrProvisionUserAsync: another concurrent request already
            // created this user's membership/organization (unique index on OrganizationId+UserId).
            _db.ChangeTracker.Clear();
            var existing = await _db.OrganizationMemberships
                .Include(m => m.Organization)
                .FirstAsync(m => m.UserId == user.UserId, ct);
            return existing.Organization!;
        }
    }
}
