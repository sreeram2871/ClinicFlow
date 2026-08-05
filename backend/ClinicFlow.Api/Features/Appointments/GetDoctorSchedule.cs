using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Appointments;

public record GetDoctorScheduleQuery(Guid DoctorId, DateTime Date) : IRequest<DoctorScheduleResponse>;

public record BookedSlot(Guid AppointmentId, DateTime Start, DateTime End, string Status, bool HasPayment, Guid PatientId, string PatientName);
public record AvailableSlot(DateTime Start, DateTime End);

public record DoctorScheduleResponse(List<BookedSlot> BookedSlots, List<AvailableSlot> AvailableSlots);

public class GetDoctorScheduleHandler : IRequestHandler<GetDoctorScheduleQuery, DoctorScheduleResponse>
{
    private readonly ClinicFlowDbContext _db;
    private static readonly TimeSpan SlotDuration = TimeSpan.FromMinutes(30);

    public GetDoctorScheduleHandler(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task<DoctorScheduleResponse> Handle(GetDoctorScheduleQuery request, CancellationToken cancellationToken)
    {
        var dayOfWeek = request.Date.DayOfWeek;
        var schedule = await _db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == request.DoctorId && s.DayOfWeek == dayOfWeek, cancellationToken);

        var dayStart = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        // ALL appointments that day, for display — includes Completed so
        // Billing can find them, and so completed visits don't just
        // silently vanish from the schedule view.
        var allAppointmentsForDay = await _db.Appointments
            .Where(a => a.DoctorId == request.DoctorId)
            .Where(a => a.Status != AppointmentStatus.Cancelled) // still hide cancelled — clutter, not useful here
            .Where(a => a.ScheduledStart >= dayStart && a.ScheduledStart < dayEnd)
            .OrderBy(a => a.ScheduledStart)
            .ToListAsync(cancellationToken);

        // Only ACTIVE (Requested/Confirmed) appointments block new bookings —
        // this is the conflict-check list, kept separate on purpose.
        var activeAppointments = allAppointmentsForDay
            .Where(a => a.Status == AppointmentStatus.Requested || a.Status == AppointmentStatus.Confirmed)
            .ToList();

        var appointmentIds = allAppointmentsForDay.Select(a => a.Id).ToList();
        var paidAppointmentIds = await _db.Payments
            .Where(p => appointmentIds.Contains(p.AppointmentId))
            .Select(p => p.AppointmentId)
            .ToListAsync(cancellationToken);

        var patientIds = allAppointmentsForDay.Select(a => a.PatientId).Distinct().ToList();
        var patientNames = await _db.Patients
            .Where(p => patientIds.Contains(p.Id))
            .Select(p => new { p.Id, p.FullName })
            .ToDictionaryAsync(p => p.Id, p => p.FullName, cancellationToken);

        var bookedSlots = allAppointmentsForDay
            .Select(a => new BookedSlot(
                a.Id, a.ScheduledStart, a.ScheduledEnd, a.Status.ToString(),
                paidAppointmentIds.Contains(a.Id),
                a.PatientId,
                patientNames.GetValueOrDefault(a.PatientId, "Unknown Patient")))
            .ToList();

        var availableSlots = new List<AvailableSlot>();

        if (schedule is not null)
        {
            var slotStart = dayStart.Add(schedule.StartTime);
            var workEnd = dayStart.Add(schedule.EndTime);

            while (slotStart.Add(SlotDuration) <= workEnd)
            {
                var slotEnd = slotStart.Add(SlotDuration);

                // Conflict check uses ONLY active appointments — a completed
                // or cancelled visit must never block a new booking.
                var overlapsBooked = activeAppointments.Any(a =>
                    a.ScheduledStart < slotEnd && a.ScheduledEnd > slotStart);

                if (!overlapsBooked)
                {
                    availableSlots.Add(new AvailableSlot(slotStart, slotEnd));
                }

                slotStart = slotEnd;
            }
        }

        return new DoctorScheduleResponse(bookedSlots, availableSlots);
    }
}