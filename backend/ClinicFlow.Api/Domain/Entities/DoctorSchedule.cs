namespace ClinicFlow.Api.Domain.Entities;

/// <summary>
/// Defines a Doctor's recurring weekly working hours (e.g. every Monday
/// 9am-5pm). One row per day-of-week the doctor works. Deliberately simple
/// — no per-date overrides or holiday exceptions in this build, per the BRD.
/// Used by the BookAppointment handler to reject bookings outside these hours.
/// </summary>
public class DoctorSchedule : TenantScopedEntity
{
    /// <summary>References User.Id where Role == Doctor.</summary>
    public Guid DoctorId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}