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
    public async Task Cancel_WhenConfirmedAndStaffRole_SucceedsAndUpdatesStatus()
    {
        var appointment = SeedAppointment(AppointmentStatus.Confirmed);
        var handler = new CancelAppointmentHandler(_db);

        await handler.Handle(new CancelAppointmentCommand(appointment.Id, Guid.NewGuid(), "Receptionist"), CancellationToken.None);

        var updated = await _db.Appointments.FindAsync(appointment.Id);
        Assert.That(updated!.Status, Is.EqualTo(AppointmentStatus.Cancelled));
    }

    [Test]
    public void Cancel_WhenAlreadyCompleted_ThrowsInvalidOperationException()
    {
        var appointment = SeedAppointment(AppointmentStatus.Completed);
        var handler = new CancelAppointmentHandler(_db);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(new CancelAppointmentCommand(appointment.Id, Guid.NewGuid(), "Receptionist"), CancellationToken.None));
    }

    [Test]
    public async Task Cancel_WhenPatientCancelsOwnAppointment_Succeeds()
    {
        var patientUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _db.Patients.Add(new Patient
        {
            Id = patientId,
            TenantId = _tenantId,
            UserId = patientUserId,
            FullName = "Test Patient",
            DateOfBirth = new DateTime(1990, 1, 1)
        });
        _db.SaveChanges();

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = AppointmentStatus.Confirmed
        };
        _db.Appointments.Add(appointment);
        _db.SaveChanges();

        var handler = new CancelAppointmentHandler(_db);

        await handler.Handle(new CancelAppointmentCommand(appointment.Id, patientUserId, "Patient"), CancellationToken.None);

        var updated = await _db.Appointments.FindAsync(appointment.Id);
        Assert.That(updated!.Status, Is.EqualTo(AppointmentStatus.Cancelled));
    }

    [Test]
    public void Cancel_WhenPatientCancelsSomeoneElsesAppointment_ThrowsForbiddenException()
    {
        var patientId = Guid.NewGuid();

        _db.Patients.Add(new Patient
        {
            Id = patientId,
            TenantId = _tenantId,
            UserId = Guid.NewGuid(), // belongs to a DIFFERENT patient's account
            FullName = "Owner Patient",
            DateOfBirth = new DateTime(1990, 1, 1)
        });
        _db.SaveChanges();

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PatientId = patientId,
            DoctorId = Guid.NewGuid(),
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = AppointmentStatus.Confirmed
        };
        _db.Appointments.Add(appointment);
        _db.SaveChanges();

        var handler = new CancelAppointmentHandler(_db);
        var differentUserId = Guid.NewGuid(); // NOT the appointment's patient's UserId

        Assert.ThrowsAsync<ClinicFlow.Api.Common.Errors.ForbiddenException>(async () =>
            await handler.Handle(new CancelAppointmentCommand(appointment.Id, differentUserId, "Patient"), CancellationToken.None));
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