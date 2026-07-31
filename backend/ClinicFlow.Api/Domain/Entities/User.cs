namespace ClinicFlow.Api.Domain.Entities;

using ClinicFlow.Api.Domain.Enums;

public class User : TenantScopedEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
