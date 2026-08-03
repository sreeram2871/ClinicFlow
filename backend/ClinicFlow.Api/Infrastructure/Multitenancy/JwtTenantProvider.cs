using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ClinicFlow.Api.Infrastructure.Multitenancy;

/// <summary>
/// Real tenant resolution: reads the "tenantId" claim embedded in the
/// caller's JWT (see JwtTokenService.GenerateAccessToken). Replaces
/// TemporaryTenantProvider, which was a hardcoded placeholder that was
/// never actually swapped out — every request has been using the same
/// fixed tenant ID regardless of who's really logged in, until now.
/// </summary>
public class JwtTenantProvider : ICurrentTenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("tenantId");

            if (claim is null || !Guid.TryParse(claim.Value, out var tenantId))
            {
                // No authenticated user with a tenantId claim — this is
                // expected for public endpoints (Login, RegisterPatient)
                // which use .IgnoreQueryFilters() and never actually read
                // this value. Returning Guid.Empty here is safe: any
                // accidental use would show zero results, never another
                // tenant's data.
                return Guid.Empty;
            }

            return tenantId;
        }
    }
}