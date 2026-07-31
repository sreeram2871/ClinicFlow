namespace ClinicFlow.Api.Domain.Entities;

/// <summary>
/// A single append-only visit note written by a Doctor about a Patient.
/// Represents clinical history — never edited or deleted once created,
/// only ever added to, so a patient's medical timeline stays accurate
/// and auditable over time.
/// </summary>
public class MedicalRecordEntry : TenantScopedEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }

    /// <summary>
    /// Optional link to the appointment this note was written during.
    /// Null when a Doctor adds a note outside a scheduled visit
    /// (e.g. reviewing a lab result later).
    /// </summary>
    public Guid? AppointmentId { get; set; }

    public string Notes { get; set; } = string.Empty;
}
