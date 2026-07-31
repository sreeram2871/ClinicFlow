using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Appointments;

public record CancelAppointmentCommand(Guid AppointmentId) : IRequest;

public class CancelAppointmentHandler : IRequestHandler<CancelAppointmentCommand>
{
    private readonly ClinicFlowDbContext _db;

    public CancelAppointmentHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel an appointment with status '{appointment.Status}'.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
    }
}