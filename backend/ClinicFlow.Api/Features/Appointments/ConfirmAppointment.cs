using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Appointments;

public record ConfirmAppointmentCommand(Guid AppointmentId) : IRequest;

public class ConfirmAppointmentHandler : IRequestHandler<ConfirmAppointmentCommand>
{
    private readonly ClinicFlowDbContext _db;

    public ConfirmAppointmentHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task Handle(ConfirmAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (appointment.Status != AppointmentStatus.Requested)
        {
            throw new InvalidOperationException($"Cannot confirm an appointment with status '{appointment.Status}'. Only Requested appointments can be confirmed.");
        }

        appointment.Status = AppointmentStatus.Confirmed;
        await _db.SaveChangesAsync(cancellationToken);
    }
}