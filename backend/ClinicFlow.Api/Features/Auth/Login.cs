using ClinicFlow.Api.Infrastructure.Auth;
using ClinicFlow.Api.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public record LoginResponse(string AccessToken, string FullName, string Role, Guid TenantId);

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly ClinicFlowDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;

    public LoginHandler(ClinicFlowDbContext db, IPasswordHasher passwordHasher, IJwtTokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .IgnoreQueryFilters() // login happens before we know the tenant — search across all tenants by email
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            // Deliberately the same generic message whether the email
            // doesn't exist, the password is wrong, or the account is
            // deactivated — never reveal which one, to avoid helping an
            // attacker enumerate valid emails.
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);

        return new LoginResponse(accessToken, user.FullName, user.Role.ToString(), user.TenantId);
    }
}