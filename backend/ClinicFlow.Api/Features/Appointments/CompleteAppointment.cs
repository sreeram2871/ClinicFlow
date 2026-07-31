using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Appointments;

public record CompleteAppointmentCommand(Guid AppointmentId, AppointmentStatus FinalStatus) : IRequest;

public class CompleteAppointmentCommandValidator : AbstractValidator<CompleteAppointmentCommand>
{
    public CompleteAppointmentCommandValidator()
    {
        RuleFor(x => x.FinalStatus)
            .Must(s => s == AppointmentStatus.Completed || s == AppointmentStatus.NoShow)
            .WithMessage("FinalStatus must be Completed or NoShow.");
    }
}

public class CompleteAppointmentHandler : IRequestHandler<CompleteAppointmentCommand>
{
    private readonly ClinicFlowDbContext _db;

    public CompleteAppointmentHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task Handle(CompleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            throw new InvalidOperationException($"Cannot complete an appointment with status '{appointment.Status}'. Only Confirmed appointments can be marked complete.");
        }

        appointment.Status = request.FinalStatus;
        await _db.SaveChangesAsync(cancellationToken);
    }
}