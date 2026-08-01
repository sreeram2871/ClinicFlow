using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ClinicFlow.Api.Common.Errors;

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

    public GetPatientRecordHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task<PatientRecordResponse> Handle(GetPatientRecordQuery request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient not found.");

        await EnforceAccessAsync(request, patient.UserId, cancellationToken);

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

    private async Task EnforceAccessAsync(GetPatientRecordQuery request, Guid? patientUserId, CancellationToken cancellationToken)
    {
        // Admin and Receptionist can view any patient in their tenant
        // (tenant isolation already handled by the global query filter).
        if (request.RequestingUserRole is "Admin" or "Receptionist")
        {
            return;
        }

        // A Patient can only view their own record.
        if (request.RequestingUserRole == "Patient")
        {
            if (patientUserId != request.RequestingUserId)
            {
                throw new ForbiddenException("You can only view your own patient record.");
            }
            return;
        }

        // A Doctor can only view patients they've actually treated —
        // proven by having at least one appointment together.
        if (request.RequestingUserRole == "Doctor")
        {
            var hasTreatedPatient = await _db.Appointments
                .AnyAsync(a => a.PatientId == request.PatientId && a.DoctorId == request.RequestingUserId, cancellationToken);

            if (!hasTreatedPatient)
            {
                throw new ForbiddenException("You can only view patients you have treated.");
            }
            return;
        }

        throw new ForbiddenException("You do not have access to this patient record.");
    }
}