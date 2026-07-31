namespace ClinicFlow.Api.Infrastructure.Multitenancy;

/// <summary>
/// TEMPORARY stand-in until the real JWT-based ICurrentTenantProvider is
/// built in the Auth feature. Returns a hardcoded Guid so the app can run
/// and be tested locally before authentication exists. Must be replaced
/// before any real multi-tenant testing.
/// </summary>
public class TemporaryTenantProvider : ICurrentTenantProvider
{
    // TODO: replace with real tenant resolution from JWT claims
    public Guid TenantId => Guid.Parse("11111111-1111-1111-1111-111111111111");
}