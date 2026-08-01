using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Features.Patients.Shared;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Infrastructure.Multitenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Patients;

public record AddMedicalRecordEntryCommand(
    Guid PatientId,
    Guid DoctorId,
    string Notes,
    Guid? AppointmentId) : IRequest<AddMedicalRecordEntryResponse>;

public record AddMedicalRecordEntryResponse(Guid RecordId);

public class AddMedicalRecordEntryCommandValidator : AbstractValidator<AddMedicalRecordEntryCommand>
{
    public AddMedicalRecordEntryCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.Notes).NotEmpty().MaximumLength(4000);
    }
}

public class AddMedicalRecordEntryHandler : IRequestHandler<AddMedicalRecordEntryCommand, AddMedicalRecordEntryResponse>
{
    private readonly ClinicFlowDbContext _db;
    private readonly PatientAccessGuard _accessGuard;
    private readonly ICurrentTenantProvider _tenantProvider;

    public AddMedicalRecordEntryHandler(ClinicFlowDbContext db, PatientAccessGuard accessGuard, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _accessGuard = accessGuard;
        _tenantProvider = tenantProvider;
    }

    public async Task<AddMedicalRecordEntryResponse> Handle(AddMedicalRecordEntryCommand request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient not found.");

        // Reuses the same guard as GetPatientRecord — since the controller
        // already restricts this endpoint to Doctors only, this call
        // specifically confirms it's THIS doctor's own treated patient.
        await _accessGuard.EnforceAsync(patient.Id, patient.UserId, request.DoctorId, "Doctor", cancellationToken);

        var record = new MedicalRecordEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            AppointmentId = request.AppointmentId,
            Notes = request.Notes
        };

        _db.MedicalRecordEntries.Add(record);
        await _db.SaveChangesAsync(cancellationToken);

        return new AddMedicalRecordEntryResponse(record.Id);
    }
}