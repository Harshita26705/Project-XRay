using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace XRay.Api.Auth;

public class DevBypassAuthOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// Development-only authentication handler that fabricates a fixed local ClaimsPrincipal so the API
/// (and this repo's own smoke tests) can be exercised before a real Microsoft Entra ID App
/// Registration is configured. Only ever wired up when Development + "UseDevAuthBypass": true —
/// see Program.cs. Never enabled in Production.
/// </summary>
public class DevBypassAuthHandler : AuthenticationHandler<DevBypassAuthOptions>
{
    public const string SchemeName = "DevBypass";

    public DevBypassAuthHandler(IOptionsMonitor<DevBypassAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-local-user"),
            new Claim(ClaimTypes.Name, "Sayyed Amaan Ali (Dev Bypass)"),
            new Claim(ClaimTypes.Email, "sayyed.amaan.ali@dev.local"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
