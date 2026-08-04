using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Infrastructure.Multitenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Appointments;

public record BookAppointmentCommand(
    Guid PatientId,
    Guid DoctorId,
    DateTime Start,
    DateTime End,
    bool BookedByStaff) : IRequest<BookAppointmentResponse>;

public record BookAppointmentResponse(Guid AppointmentId, string Status);

public class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.Start).LessThan(x => x.End).WithMessage("Start time must be before end time.");
        RuleFor(x => x.Start).GreaterThan(DateTime.UtcNow).WithMessage("Cannot book an appointment in the past.");
    }
}

public class BookAppointmentHandler : IRequestHandler<BookAppointmentCommand, BookAppointmentResponse>
{
    private readonly ClinicFlowDbContext _db;
    private readonly ICurrentTenantProvider _tenantProvider;

    public BookAppointmentHandler(ClinicFlowDbContext db, ICurrentTenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<BookAppointmentResponse> Handle(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        // Normalize incoming DateTimes to UTC-kind once, up front — request.Start/
        // request.End arrive from JSON deserialization with Kind=Unspecified,
        // which PostgreSQL refuses to accept in a timestamp with time zone column.
        var start = DateTime.SpecifyKind(request.Start, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(request.End, DateTimeKind.Utc);

        // Rule 1: must fall within the doctor's working hours for that day
        var dayOfWeek = start.DayOfWeek;
        var schedule = await _db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == request.DoctorId && s.DayOfWeek == dayOfWeek, cancellationToken);

        if (schedule is null)
        {
            throw new ArgumentException("Doctor does not work on this day.");
        }

        var requestedStartTime = start.TimeOfDay;
        var requestedEndTime = end.TimeOfDay;

        if (requestedStartTime < schedule.StartTime || requestedEndTime > schedule.EndTime)
        {
            throw new ArgumentException("Requested time is outside the doctor's working hours.");
        }

        // Rule 2: no overlap with an existing active appointment for this doctor
        var hasConflict = await _db.Appointments
            .Where(a => a.DoctorId == request.DoctorId)
            .Where(a => a.Status == AppointmentStatus.Requested || a.Status == AppointmentStatus.Confirmed)
            .AnyAsync(a => a.ScheduledStart < end && a.ScheduledEnd > start, cancellationToken);

        if (hasConflict)
        {
            throw new InvalidOperationException("This doctor already has an appointment in that time slot.");
        }

        // Rule 3: status depends on who's booking (per BRD)
        var status = request.BookedByStaff ? AppointmentStatus.Confirmed : AppointmentStatus.Requested;

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            ScheduledStart = start,
            ScheduledEnd = end,
            Status = status
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);

        return new BookAppointmentResponse(appointment.Id, appointment.Status.ToString());
    }
}