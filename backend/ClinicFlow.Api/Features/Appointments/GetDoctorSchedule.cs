using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Appointments;

public record GetDoctorScheduleQuery(Guid DoctorId, DateTime Date) : IRequest<DoctorScheduleResponse>;

public record BookedSlot(Guid AppointmentId, DateTime Start, DateTime End, string Status);
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

        var dayStart = request.Date.Date;
        var dayEnd = dayStart.AddDays(1);

        var bookedAppointments = await _db.Appointments
            .Where(a => a.DoctorId == request.DoctorId)
            .Where(a => a.Status == AppointmentStatus.Requested || a.Status == AppointmentStatus.Confirmed)
            .Where(a => a.ScheduledStart >= dayStart && a.ScheduledStart < dayEnd)
            .OrderBy(a => a.ScheduledStart)
            .ToListAsync(cancellationToken);

        var bookedSlots = bookedAppointments
            .Select(a => new BookedSlot(a.Id, a.ScheduledStart, a.ScheduledEnd, a.Status.ToString()))
            .ToList();

        var availableSlots = new List<AvailableSlot>();

        if (schedule is not null)
        {
            var slotStart = dayStart.Add(schedule.StartTime);
            var workEnd = dayStart.Add(schedule.EndTime);

            while (slotStart.Add(SlotDuration) <= workEnd)
            {
                var slotEnd = slotStart.Add(SlotDuration);

                var overlapsBooked = bookedAppointments.Any(a =>
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