namespace XRay.Api.Auth;

/// <summary>Extracted so the fail-fast rule can be unit tested without booting the full host.</summary>
public static class DevAuthBypassGuard
{
    public static void EnsureNotEnabledOutsideDevelopment(bool isDevelopmentEnvironment, bool useDevAuthBypassConfigured)
    {
        if (!isDevelopmentEnvironment && useDevAuthBypassConfigured)
        {
            throw new InvalidOperationException("UseDevAuthBypass must never be set outside the Development environment.");
        }
    }
}
