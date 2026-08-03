using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Patients;

public record GetMyPatientRecordQuery(Guid RequestingUserId) : IRequest<PatientRecordResponse>;

public class GetMyPatientRecordHandler : IRequestHandler<GetMyPatientRecordQuery, PatientRecordResponse>
{
    private readonly ClinicFlowDbContext _db;

    public GetMyPatientRecordHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task<PatientRecordResponse> Handle(GetMyPatientRecordQuery request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.UserId == request.RequestingUserId, cancellationToken)
            ?? throw new KeyNotFoundException("No patient record linked to this account.");

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