namespace ClinicFlow.Api.Domain.Entities;

public class Patient : TenantScopedEntity
{
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
