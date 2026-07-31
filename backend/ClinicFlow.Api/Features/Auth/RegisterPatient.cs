using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Auth;
using ClinicFlow.Api.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Auth;

public record RegisterPatientCommand(
    Guid TenantId,
    string FullName,
    string Email,
    string Password,
    string Phone,
    DateTime DateOfBirth) : IRequest<RegisterPatientResponse>;

public record RegisterPatientResponse(Guid PatientId, Guid UserId);

public class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.UtcNow);
    }
}

public class RegisterPatientHandler : IRequestHandler<RegisterPatientCommand, RegisterPatientResponse>
{
    private readonly ClinicFlowDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterPatientHandler(ClinicFlowDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterPatientResponse> Handle(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        var emailTaken = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.TenantId == request.TenantId && u.Email == request.Email, cancellationToken);

        if (emailTaken)
        {
            throw new ArgumentException("An account with this email already exists for this clinic.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Patient,
            IsActive = true
        };

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = user.Id,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone,
            Email = request.Email
        };

        _db.Users.Add(user);
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(cancellationToken);

        return new RegisterPatientResponse(patient.Id, user.Id);
    }
}