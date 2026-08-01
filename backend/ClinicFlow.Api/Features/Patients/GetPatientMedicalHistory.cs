using ClinicFlow.Api.Features.Patients.Shared;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Patients;

public record GetPatientMedicalHistoryQuery(Guid PatientId, Guid RequestingUserId, string RequestingUserRole)
    : IRequest<List<MedicalRecordEntryResponse>>;

public record MedicalRecordEntryResponse(Guid Id, string Notes, Guid DoctorId, Guid? AppointmentId, DateTime CreatedAt);

public class GetPatientMedicalHistoryHandler : IRequestHandler<GetPatientMedicalHistoryQuery, List<MedicalRecordEntryResponse>>
{
    private readonly ClinicFlowDbContext _db;
    private readonly PatientAccessGuard _accessGuard;

    public GetPatientMedicalHistoryHandler(ClinicFlowDbContext db, PatientAccessGuard accessGuard)
    {
        _db = db;
        _accessGuard = accessGuard;
    }

    public async Task<List<MedicalRecordEntryResponse>> Handle(GetPatientMedicalHistoryQuery request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient not found.");

        await _accessGuard.EnforceAsync(patient.Id, patient.UserId, request.RequestingUserId, request.RequestingUserRole, cancellationToken);

        return await _db.MedicalRecordEntries
            .Where(m => m.PatientId == request.PatientId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MedicalRecordEntryResponse(m.Id, m.Notes, m.DoctorId, m.AppointmentId, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}