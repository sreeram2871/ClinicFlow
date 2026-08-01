using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Features.Appointments;
using ClinicFlow.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace ClinicFlow.Api.Tests.Features.Appointments;

public class GetDoctorScheduleHandlerTests
{
    private ClinicFlowDbContext _db = null!;
    private Guid _tenantId;
    private Guid _doctorId;

    [SetUp]
    public void Setup()
    {
        _tenantId = Guid.NewGuid();
        _doctorId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ClinicFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ClinicFlowDbContext(options, new FakeTenantProvider(_tenantId));
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Handle_WithOneBookedSlot_ExcludesItFromAvailableSlots()
    {
        var nextMonday = NextMonday();

        _db.DoctorSchedules.Add(new DoctorSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DoctorId = _doctorId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 0, 0) // deliberately short: only two 30-min slots exist (9-9:30, 9:30-10)
        });

        _db.Appointments.Add(new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PatientId = Guid.NewGuid(),
            DoctorId = _doctorId,
            ScheduledStart = nextMonday.AddHours(9),
            ScheduledEnd = nextMonday.AddHours(9).AddMinutes(30),
            Status = AppointmentStatus.Confirmed
        });

        await _db.SaveChangesAsync();

        var handler = new GetDoctorScheduleHandler(_db);
        var result = await handler.Handle(new GetDoctorScheduleQuery(_doctorId, nextMonday), CancellationToken.None);

        Assert.That(result.BookedSlots.Count, Is.EqualTo(1));
        Assert.That(result.AvailableSlots.Count, Is.EqualTo(1)); // only the 9:30-10:00 slot remains
        Assert.That(result.AvailableSlots[0].Start, Is.EqualTo(nextMonday.AddHours(9).AddMinutes(30)));
    }

    [Test]
    public async Task Handle_WithNoScheduleForThatDay_ReturnsEmptyAvailableSlots()
    {
        var sunday = NextMonday().AddDays(-1); // no DoctorSchedule seeded for Sunday at all

        var handler = new GetDoctorScheduleHandler(_db);
        var result = await handler.Handle(new GetDoctorScheduleQuery(_doctorId, sunday), CancellationToken.None);

        Assert.That(result.AvailableSlots, Is.Empty);
        Assert.That(result.BookedSlots, Is.Empty);
    }

    private static DateTime NextMonday()
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        daysUntilMonday = daysUntilMonday == 0 ? 7 : daysUntilMonday;
        return today.AddDays(daysUntilMonday);
    }
}