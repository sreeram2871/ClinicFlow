using ClinicFlow.Api.Domain.Enums;

namespace ClinicFlow.Api.Domain.Entities;

public class Appointment : TenantScopedEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Requested;
}