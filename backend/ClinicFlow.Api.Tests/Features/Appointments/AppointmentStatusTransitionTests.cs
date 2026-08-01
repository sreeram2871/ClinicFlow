using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Features.Appointments;
using ClinicFlow.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace ClinicFlow.Api.Tests.Features.Appointments;

public class AppointmentStatusTransitionTests
{
    private ClinicFlowDbContext _db = null!;
    private Guid _tenantId;

    [SetUp]
    public void Setup()
    {
        _tenantId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ClinicFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ClinicFlowDbContext(options, new FakeTenantProvider(_tenantId));
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private Appointment SeedAppointment(AppointmentStatus status)
    {
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = status
        };
        _db.Appointments.Add(appointment);
        _db.SaveChanges();
        return appointment;
    }

    // --- Confirm ---

    [Test]
    public async Task Confirm_WhenRequested_SucceedsAndUpdatesStatus()
    {
        var appointment = SeedAppointment(AppointmentStatus.Requested);
        var handler = new ConfirmAppointmentHandler(_db);

        await handler.Handle(new ConfirmAppointmentCommand(appointment.Id), CancellationToken.None);

        var updated = await _db.Appointments.FindAsync(appointment.Id);
        Assert.That(updated!.Status, Is.EqualTo(AppointmentStatus.Confirmed));
    }

    [Test]
    public void Confirm_WhenAlreadyConfirmed_ThrowsInvalidOperationException()
    {
        var appointment = SeedAppointment(AppointmentStatus.Confirmed);
        var handler = new ConfirmAppointmentHandler(_db);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(new ConfirmAppointmentCommand(appointment.Id), CancellationToken.None));
    }

    // --- Cancel ---

    [Test]
    public async Task Cancel_WhenConfirmed_SucceedsAndUpdatesStatus()
    {
        var appointment = SeedAppointment(AppointmentStatus.Confirmed);
        var handler = new CancelAppointmentHandler(_db);

        await handler.Handle(new CancelAppointmentCommand(appointment.Id), CancellationToken.None);

        var updated = await _db.Appointments.FindAsync(appointment.Id);
        Assert.That(updated!.Status, Is.EqualTo(AppointmentStatus.Cancelled));
    }

    [Test]
    public void Cancel_WhenAlreadyCompleted_ThrowsInvalidOperationException()
    {
        var appointment = SeedAppointment(AppointmentStatus.Completed);
        var handler = new CancelAppointmentHandler(_db);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(new CancelAppointmentCommand(appointment.Id), CancellationToken.None));
    }

    // --- Complete ---

    [Test]
    public async Task Complete_WhenConfirmed_SucceedsAndUpdatesStatus()
    {
        var appointment = SeedAppointment(AppointmentStatus.Confirmed);
        var handler = new CompleteAppointmentHandler(_db);

        await handler.Handle(new CompleteAppointmentCommand(appointment.Id, AppointmentStatus.Completed), CancellationToken.None);

        var updated = await _db.Appointments.FindAsync(appointment.Id);
        Assert.That(updated!.Status, Is.EqualTo(AppointmentStatus.Completed));
    }

    [Test]
    public void Complete_WhenStillRequested_ThrowsInvalidOperationException()
    {
        var appointment = SeedAppointment(AppointmentStatus.Requested);
        var handler = new CompleteAppointmentHandler(_db);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(new CompleteAppointmentCommand(appointment.Id, AppointmentStatus.Completed), CancellationToken.None));
    }
}