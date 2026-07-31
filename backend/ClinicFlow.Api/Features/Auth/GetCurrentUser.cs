using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Auth;

public record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserResponse>;

public record CurrentUserResponse(Guid Id, string FullName, string Email, string Role, Guid TenantId);

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    private readonly ClinicFlowDbContext _db;

    public GetCurrentUserHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task<CurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        return new CurrentUserResponse(user.Id, user.FullName, user.Email, user.Role.ToString(), user.TenantId);
    }
}