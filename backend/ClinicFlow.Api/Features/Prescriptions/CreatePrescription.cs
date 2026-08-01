using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Features.Patients.Shared;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Infrastructure.Multitenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Prescriptions;

public record CreatePrescriptionCommand(
    Guid PatientId,
    Guid DoctorId,
    string MedicineName,
    string Dosage,
    string? Notes) : IRequest<CreatePrescriptionResponse>;

public record CreatePrescriptionResponse(Guid PrescriptionId);

public class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.MedicineName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(200);
    }
}

public class CreatePrescriptionHandler : IRequestHandler<CreatePrescriptionCommand, CreatePrescriptionResponse>
{
    private readonly ClinicFlowDbContext _db;
    private readonly PatientAccessGuard _accessGuard;
    private readonly ICurrentTenantProvider _tenantProvider;

    public CreatePrescriptionHandler(ClinicFlowDbContext db, PatientAccessGuard accessGuard, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _accessGuard = accessGuard;
        _tenantProvider = tenantProvider;
    }

    public async Task<CreatePrescriptionResponse> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient not found.");

        await _accessGuard.EnforceAsync(patient.Id, patient.UserId, request.DoctorId, "Doctor", cancellationToken);

        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            MedicineName = request.MedicineName,
            Dosage = request.Dosage,
            Notes = request.Notes ?? string.Empty
        };

        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreatePrescriptionResponse(prescription.Id);
    }
}