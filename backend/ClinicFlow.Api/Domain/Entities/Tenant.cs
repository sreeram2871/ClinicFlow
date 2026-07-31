namespace ClinicFlow.Api.Domain.Entities;

public class Tenant  : AuditableEntity
{
    public string ClinicName { get; set; } = string.Empty;
}
