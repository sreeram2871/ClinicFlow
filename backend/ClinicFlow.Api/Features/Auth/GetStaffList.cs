using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Auth;

public record GetStaffListQuery : IRequest<List<StaffMemberResponse>>;

public record StaffMemberResponse(Guid Id, string FullName, string Email, string Role, bool IsActive);

public class GetStaffListHandler : IRequestHandler<GetStaffListQuery, List<StaffMemberResponse>>
{
    private readonly ClinicFlowDbContext _db;

    public GetStaffListHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task<List<StaffMemberResponse>> Handle(GetStaffListQuery request, CancellationToken cancellationToken)
    {
        return await _db.Users
            .Where(u => u.Role != UserRole.Patient)
            .OrderBy(u => u.FullName)
            .Select(u => new StaffMemberResponse(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive))
            .ToListAsync(cancellationToken);
    }
}