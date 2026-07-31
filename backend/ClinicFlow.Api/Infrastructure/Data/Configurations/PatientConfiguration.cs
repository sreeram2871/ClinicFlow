using ClinicFlow.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicFlow.Api.Infrastructure.Data.Configurations;

/// <summary>
/// No unique index on Email/Phone here — unlike User, two patients could
/// plausibly share contact details in real life (e.g. a parent registering
/// multiple children under one household phone number).
/// </summary>
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.Property(p => p.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Phone)
            .HasMaxLength(20);

        builder.Property(p => p.Email)
            .HasMaxLength(256);
    }
}