using ClinicFlow.Api.Domain.Enums;

namespace ClinicFlow.Api.Domain.Entities;

/// <summary>
/// A manually recorded payment against a completed Appointment.
/// There is no online payment gateway in this build — a Receptionist
/// enters the amount collected after the fact.
/// </summary>
public class Payment : TenantScopedEntity
{
    public Guid AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}