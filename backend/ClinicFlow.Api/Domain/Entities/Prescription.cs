namespace ClinicFlow.Api.Domain.Entities;

/// <summary>
/// A simple text-based prescription written by a Doctor for a Patient.
/// Scoped deliberately to free-text medicine/dosage/notes fields (no
/// structured drug database or PDF generation) — this is explicitly
/// out of scope per the BRD to keep this MVP build achievable.
/// </summary>
public class Prescription : TenantScopedEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}