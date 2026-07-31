namespace ClinicFlow.Api.Domain.Entities;

public abstract class TenantScopedEntity : AuditableEntity
{
    public Guid TenantId { get; set; }
}
