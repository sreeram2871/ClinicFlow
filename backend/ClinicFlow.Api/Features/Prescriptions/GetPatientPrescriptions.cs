using ClinicFlow.Api.Features.Patients.Shared;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Prescriptions;

public record GetPatientPrescriptionsQuery(Guid PatientId, Guid RequestingUserId, string RequestingUserRole)
    : IRequest<List<PrescriptionResponse>>;

public record PrescriptionResponse(Guid Id, string MedicineName, string Dosage, string Notes, Guid DoctorId, DateTime CreatedAt);

public class GetPatientPrescriptionsHandler : IRequestHandler<GetPatientPrescriptionsQuery, List<PrescriptionResponse>>
{
    private readonly ClinicFlowDbContext _db;
    private readonly PatientAccessGuard _accessGuard;

    public GetPatientPrescriptionsHandler(ClinicFlowDbContext db, PatientAccessGuard accessGuard)
    {
        _db = db;
        _accessGuard = accessGuard;
    }

    public async Task<List<PrescriptionResponse>> Handle(GetPatientPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient not found.");

        await _accessGuard.EnforceAsync(patient.Id, patient.UserId, request.RequestingUserId, request.RequestingUserRole, cancellationToken);

        return await _db.Prescriptions
            .Where(p => p.PatientId == request.PatientId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PrescriptionResponse(p.Id, p.MedicineName, p.Dosage, p.Notes, p.DoctorId, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}