using ClinicFlow.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicFlow.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Tenant is the root entity — no TenantId to worry about here, just
/// basic constraints on the clinic's name.
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(t => t.ClinicName)
            .IsRequired()
            .HasMaxLength(200);
    }
}