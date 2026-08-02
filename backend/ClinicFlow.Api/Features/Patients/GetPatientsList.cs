using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Patients;

public record GetPatientsListQuery(Guid RequestingUserId, string RequestingUserRole) : IRequest<List<PatientListItemResponse>>;

public record PatientListItemResponse(Guid Id, string FullName, DateTime DateOfBirth, string Phone);

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

        // Admin and Receptionist see every patient in the tenant (already
        // scoped by the global query filter). A Doctor only sees patients
        // they've actually treated — same rule PatientAccessGuard enforces
        // for individual lookups, applied here at list-level via a join
        // against Appointments instead of a per-patient check.
        if (request.RequestingUserRole == "Doctor")
        {
            query = query.Where(p => _db.Appointments
                .Any(a => a.PatientId == p.Id && a.DoctorId == request.RequestingUserId));
        }

        return await query
            .OrderBy(p => p.FullName)
            .Select(p => new PatientListItemResponse(p.Id, p.FullName, p.DateOfBirth, p.Phone))
            .ToListAsync(cancellationToken);
    }
}