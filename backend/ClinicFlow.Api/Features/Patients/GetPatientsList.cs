using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Patients;

public record GetPatientsListQuery(Guid RequestingUserId, string RequestingUserRole) : IRequest<List<PatientListItemResponse>>;

public record PatientListItemResponse(Guid Id, string FullName, DateTime DateOfBirth, string Phone, DateTime? LastVisitDate);

public class GetPatientsListHandler : IRequestHandler<GetPatientsListQuery, List<PatientListItemResponse>>
{
    private readonly ClinicFlowDbContext _db;

    public GetPatientsListHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task<List<PatientListItemResponse>> Handle(GetPatientsListQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Patients.AsQueryable();

        if (request.RequestingUserRole == "Doctor")
        {
            query = query.Where(p => _db.Appointments
                .Any(a => a.PatientId == p.Id && a.DoctorId == request.RequestingUserId));
        }

        var patients = await query
            .OrderBy(p => p.FullName)
            .ToListAsync(cancellationToken);

        // One grouped lookup for ALL patients' last Completed visit, not
        // a separate query per patient — avoids N+1 at list scale.
        var patientIds = patients.Select(p => p.Id).ToList();
        var lastVisits = await _db.Appointments
            .Where(a => patientIds.Contains(a.PatientId) && a.Status == AppointmentStatus.Completed)
            .GroupBy(a => a.PatientId)
            .Select(g => new { PatientId = g.Key, LastVisit = g.Max(a => a.ScheduledStart) })
            .ToDictionaryAsync(x => x.PatientId, x => x.LastVisit, cancellationToken);

        return patients
            .Select(p => new PatientListItemResponse(
                p.Id, p.FullName, p.DateOfBirth, p.Phone,
                lastVisits.GetValueOrDefault(p.Id)))
            .ToList();
    }
}