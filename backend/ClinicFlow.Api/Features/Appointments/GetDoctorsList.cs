using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Appointments;

public record GetDoctorsListQuery : IRequest<List<DoctorListItemResponse>>;

public record DoctorListItemResponse(Guid Id, string FullName);

public class GetDoctorsListHandler : IRequestHandler<GetDoctorsListQuery, List<DoctorListItemResponse>>
{
    private readonly ClinicFlowDbContext _db;

    public GetDoctorsListHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task<List<DoctorListItemResponse>> Handle(GetDoctorsListQuery request, CancellationToken cancellationToken)
    {
        return await _db.Users
            .Where(u => u.Role == UserRole.Doctor && u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new DoctorListItemResponse(u.Id, u.FullName))
            .ToListAsync(cancellationToken);
    }
}