using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Appointments;

public record CancelAppointmentCommand(Guid AppointmentId, Guid RequestingUserId, string RequestingUserRole) : IRequest;

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

        // Staff (Admin/Doctor/Receptionist) can cancel any appointment in
        // their tenant, unchanged from before. A Patient can only cancel
        // their OWN appointment — verified by checking the appointment's
        // patient is actually linked to this Patient's own account.
        if (request.RequestingUserRole == "Patient")
        {
            var isOwnAppointment = await _db.Patients
                .AnyAsync(p => p.Id == appointment.PatientId && p.UserId == request.RequestingUserId, cancellationToken);

            if (!isOwnAppointment)
            {
                throw new ClinicFlow.Api.Common.Errors.ForbiddenException("You can only cancel your own appointments.");
            }
        }

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel an appointment with status '{appointment.Status}'.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
    }
}