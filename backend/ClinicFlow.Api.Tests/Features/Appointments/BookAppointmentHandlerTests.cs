using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Features.Appointments;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Infrastructure.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using NUnit.Framework;

namespace ClinicFlow.Api.Tests.Features.Appointments;

public class BookAppointmentHandlerTests
{
    private ClinicFlowDbContext _db = null!;
    private Guid _tenantId;
    private Guid _doctorId;
    private Guid _patientId;

    [SetUp]
    public void Setup()
    {
        _tenantId = Guid.NewGuid();
        _doctorId = Guid.NewGuid();
        _patientId = Guid.NewGuid();

        var fakeTenantProvider = new FakeTenantProvider(_tenantId);

        var options = new DbContextOptionsBuilder<ClinicFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB name per test — full isolation
            .Options;

        _db = new ClinicFlowDbContext(options, fakeTenantProvider);

        // Seed a doctor's working hours: every Monday, 9am-5pm
        _db.DoctorSchedules.Add(new DoctorSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DoctorId = _doctorId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        });

        _db.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task Handle_WhenSlotIsFree_BooksAppointmentSuccessfully()
    {
        // Arrange
        var handler = new BookAppointmentHandler(_db, new FakeTenantProvider(_tenantId));
        var mondayAt10Am = NextMonday().AddHours(10);

        var command = new BookAppointmentCommand(_patientId, _doctorId, mondayAt10Am, mondayAt10Am.AddMinutes(30), BookedByStaff: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Status, Is.EqualTo("Confirmed"));
        Assert.That(await _db.Appointments.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void Handle_WhenSlotAlreadyBooked_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new BookAppointmentHandler(_db, new FakeTenantProvider(_tenantId));
        var mondayAt10Am = NextMonday().AddHours(10);

        _db.Appointments.Add(new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PatientId = Guid.NewGuid(),
            DoctorId = _doctorId,
            ScheduledStart = mondayAt10Am,
            ScheduledEnd = mondayAt10Am.AddMinutes(30),
            Status = AppointmentStatus.Confirmed
        });
        _db.SaveChanges();

        var conflictingCommand = new BookAppointmentCommand(_patientId, _doctorId, mondayAt10Am, mondayAt10Am.AddMinutes(30), BookedByStaff: true);

        // Act + Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(conflictingCommand, CancellationToken.None));
    }

    [Test]
    public void Handle_WhenOutsideWorkingHours_ThrowsArgumentException()
    {
        // Arrange
        var handler = new BookAppointmentHandler(_db, new FakeTenantProvider(_tenantId));
        var mondayAt8Am = NextMonday().AddHours(8); // before 9am start

        var command = new BookAppointmentCommand(_patientId, _doctorId, mondayAt8Am, mondayAt8Am.AddMinutes(30), BookedByStaff: true);

        // Act + Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public async Task Handle_WhenBookedByPatient_StatusIsRequestedNotConfirmed()
    {
        // Arrange
        var handler = new BookAppointmentHandler(_db, new FakeTenantProvider(_tenantId));
        var mondayAt10Am = NextMonday().AddHours(10);

        var command = new BookAppointmentCommand(_patientId, _doctorId, mondayAt10Am, mondayAt10Am.AddMinutes(30), BookedByStaff: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Status, Is.EqualTo("Requested"));
    }

    private static DateTime NextMonday()
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        daysUntilMonday = daysUntilMonday == 0 ? 7 : daysUntilMonday; // always a FUTURE Monday, never today
        return today.AddDays(daysUntilMonday);
    }
}

/// <summary>Fake tenant provider for tests — no HTTP context needed.</summary>
public class FakeTenantProvider : ICurrentTenantProvider
{
    public Guid TenantId { get; }
    public FakeTenantProvider(Guid tenantId) => TenantId = tenantId;
}