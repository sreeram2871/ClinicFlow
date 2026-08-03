using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Infrastructure.Multitenancy;
using FluentValidation;
using MediatR;

namespace ClinicFlow.Api.Features.Patients;

public record RegisterWalkInPatientCommand(
    string FullName,
    DateTime DateOfBirth,
    string Phone,
    string Email) : IRequest<RegisterWalkInPatientResponse>;

public record RegisterWalkInPatientResponse(Guid PatientId);

public class RegisterWalkInPatientCommandValidator : AbstractValidator<RegisterWalkInPatientCommand>
{
    public RegisterWalkInPatientCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.UtcNow);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(256);
    }
}

public class RegisterWalkInPatientHandler : IRequestHandler<RegisterWalkInPatientCommand, RegisterWalkInPatientResponse>
{
    private readonly ClinicFlowDbContext _db;
    private readonly ICurrentTenantProvider _tenantProvider;

    public RegisterWalkInPatientHandler(ClinicFlowDbContext db, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<RegisterWalkInPatientResponse> Handle(RegisterWalkInPatientCommand request, CancellationToken cancellationToken)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            UserId = null, // walk-in patient — no portal login, per the BRD
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone,
            Email = request.Email
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(cancellationToken);

        return new RegisterWalkInPatientResponse(patient.Id);
    }
}