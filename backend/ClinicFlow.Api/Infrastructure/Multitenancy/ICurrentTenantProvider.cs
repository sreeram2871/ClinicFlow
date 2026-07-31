namespace ClinicFlow.Api.Infrastructure.Multitenancy;

/// <summary>
/// Provides the current request's TenantId to anything that needs it —
/// most importantly, ClinicFlowDbContext's global query filter. The real
/// implementation reads this from the logged-in user's JWT claims; a fake
/// implementation can be swapped in for unit tests without touching HTTP.
/// </summary>
public interface ICurrentTenantProvider
{
    Guid TenantId { get; }
}