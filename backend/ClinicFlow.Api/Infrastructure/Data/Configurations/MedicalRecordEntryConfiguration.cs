using ClinicFlow.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicFlow.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Notes get a generous max length since these are free-text clinical
/// notes, not short labels — 4000 chars is comfortably enough for a
/// detailed visit note without going unbounded.
/// </summary>
public class MedicalRecordEntryConfiguration : IEntityTypeConfiguration<MedicalRecordEntry>
{
    public void Configure(EntityTypeBuilder<MedicalRecordEntry> builder)
    {
        builder.Property(m => m.Notes)
            .IsRequired()
            .HasMaxLength(4000);
    }
}