using ClinicFlow.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicFlow.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Tells EF Core how to store User rows: required fields, string limits,
/// and a uniqueness rule so two staff accounts can't share an email
/// within the same clinic.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        // Same email can exist in two different clinics, but not twice
        // within the same clinic.
        builder.HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique();
    }
}