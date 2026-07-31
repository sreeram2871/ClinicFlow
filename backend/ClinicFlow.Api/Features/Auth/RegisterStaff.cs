using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Auth;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Infrastructure.Multitenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Auth;

public record RegisterStaffCommand(
    string FullName,
    string Email,
    string Password,
    UserRole Role) : IRequest<RegisterStaffResponse>;

public record RegisterStaffResponse(Guid UserId);

public class RegisterStaffCommandValidator : AbstractValidator<RegisterStaffCommand>
{
    public RegisterStaffCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role)
            .Must(r => r == UserRole.Doctor || r == UserRole.Receptionist || r == UserRole.Admin)
            .WithMessage("Role must be Admin, Doctor, or Receptionist.");
    }
}

public class RegisterStaffHandler : IRequestHandler<RegisterStaffCommand, RegisterStaffResponse>
{
    private readonly ClinicFlowDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentTenantProvider _tenantProvider;

    public RegisterStaffHandler(ClinicFlowDbContext db, IPasswordHasher passwordHasher, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tenantProvider = tenantProvider;
    }

    public async Task<RegisterStaffResponse> Handle(RegisterStaffCommand request, CancellationToken cancellationToken)
    {
        var emailTaken = await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailTaken)
        {
            throw new ArgumentException("An account with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return new RegisterStaffResponse(user.Id);
    }
}