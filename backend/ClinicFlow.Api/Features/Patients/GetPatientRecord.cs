using ClinicFlow.Api.Features.Patients.Shared;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Patients;

public record GetPatientRecordQuery(Guid PatientId, Guid RequestingUserId, string RequestingUserRole)
    : IRequest<PatientRecordResponse>;

public record PatientRecordResponse(
    Guid Id,
    string FullName,
    DateTime DateOfBirth,
    string Phone,
    string Email,
    List<RecentAppointment> RecentAppointments);

public record RecentAppointment(Guid Id, DateTime ScheduledStart, string Status);

public class GetPatientRecordHandler : IRequestHandler<GetPatientRecordQuery, PatientRecordResponse>
{
    private readonly ClinicFlowDbContext _db;
    private readonly PatientAccessGuard _accessGuard;

    public GetPatientRecordHandler(ClinicFlowDbContext db, PatientAccessGuard accessGuard)
    {
        _db = db;
        _accessGuard = accessGuard;
    }

    public async Task<PatientRecordResponse> Handle(GetPatientRecordQuery request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient not found.");

        await _accessGuard.EnforceAsync(patient.Id, patient.UserId, request.RequestingUserId, request.RequestingUserRole, cancellationToken);

        var recentAppointments = await _db.Appointments
            .Where(a => a.PatientId == patient.Id)
            .OrderByDescending(a => a.ScheduledStart)
            .Take(10)
            .Select(a => new RecentAppointment(a.Id, a.ScheduledStart, a.Status.ToString()))
            .ToListAsync(cancellationToken);

        return new PatientRecordResponse(
            patient.Id, patient.FullName, patient.DateOfBirth,
            patient.Phone, patient.Email, recentAppointments);
    }
}