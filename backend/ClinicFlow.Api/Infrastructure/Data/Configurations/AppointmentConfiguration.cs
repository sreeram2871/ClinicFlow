using ClinicFlow.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicFlow.Api.Infrastructure.Data.Configurations;

/// <summary>
/// The DoctorId + ScheduledStart index isn't a uniqueness rule — it's a
/// performance index. Booking-conflict checks ("does this doctor already
/// have an appointment near this time?") run on nearly every booking, so
/// this index makes that lookup fast instead of scanning the whole table.
/// </summary>
public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.Property(a => a.ScheduledStart)
            .IsRequired();

        builder.Property(a => a.ScheduledEnd)
            .IsRequired();

        builder.HasIndex(a => new { a.DoctorId, a.ScheduledStart });
    }
}