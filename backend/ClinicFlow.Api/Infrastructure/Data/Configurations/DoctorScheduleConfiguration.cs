using ClinicFlow.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicFlow.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Ensures a doctor can't have two overlapping working-hour rows for the
/// same day, and that TimeSpan values map cleanly to SQL Server's time type.
/// </summary>
public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.Property(d => d.StartTime)
            .IsRequired();

        builder.Property(d => d.EndTime)
            .IsRequired();

        // One doctor can only have one schedule row per day of week —
        // prevents accidentally creating two conflicting Monday entries.
        builder.HasIndex(d => new { d.DoctorId, d.DayOfWeek })
            .IsUnique();
    }
}