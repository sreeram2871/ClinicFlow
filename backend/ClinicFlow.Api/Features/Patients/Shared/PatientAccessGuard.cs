using ClinicFlow.Api.Common.Errors;
using ClinicFlow.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Patients.Shared;

/// <summary>
/// Centralizes the "can this user access this patient's data?" rule so it
/// isn't duplicated across GetPatientRecord, AddMedicalRecordEntry, and any
/// future feature that touches patient data. One rule, one place to fix it.
/// </summary>
public class PatientAccessGuard
{
    private readonly ClinicFlowDbContext _db;

    public PatientAccessGuard(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task EnforceAsync(Guid patientId, Guid? patientUserId, Guid requestingUserId, string requestingUserRole, CancellationToken cancellationToken)
    {
        if (requestingUserRole is "Admin" or "Receptionist")
        {
            return;
        }

        if (requestingUserRole == "Patient")
        {
            if (patientUserId != requestingUserId)
            {
                throw new ForbiddenException("You can only access your own patient record.");
            }
            return;
        }

        if (requestingUserRole == "Doctor")
        {
            var hasTreatedPatient = await _db.Appointments
                .AnyAsync(a => a.PatientId == patientId && a.DoctorId == requestingUserId, cancellationToken);

            if (!hasTreatedPatient)
            {
                throw new ForbiddenException("You can only access patients you have treated.");
            }
            return;
        }

        throw new ForbiddenException("You do not have access to this patient's data.");
    }
}