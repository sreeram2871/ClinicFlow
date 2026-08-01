using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Features.Billing;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Tests.Features.Appointments;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace ClinicFlow.Api.Tests.Features.Billing;

public class RecordPaymentHandlerTests
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
            ScheduledStart = DateTime.UtcNow.AddDays(-1),
            ScheduledEnd = DateTime.UtcNow.AddDays(-1).AddMinutes(30),
            Status = status
        };
        _db.Appointments.Add(appointment);
        _db.SaveChanges();
        return appointment;
    }

    [Test]
    public async Task Handle_ForCompletedAppointment_RecordsPaymentSuccessfully()
    {
        var appointment = SeedAppointment(AppointmentStatus.Completed);
        var handler = new RecordPaymentHandler(_db, new FakeTenantProvider(_tenantId));

        var result = await handler.Handle(
            new RecordPaymentCommand(appointment.Id, 500m, PaymentMethod.Cash), CancellationToken.None);

        Assert.That(result.PaymentId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(await _db.Payments.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void Handle_ForNonCompletedAppointment_ThrowsInvalidOperationException()
    {
        var appointment = SeedAppointment(AppointmentStatus.Confirmed);
        var handler = new RecordPaymentHandler(_db, new FakeTenantProvider(_tenantId));

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(new RecordPaymentCommand(appointment.Id, 500m, PaymentMethod.Cash), CancellationToken.None));
    }

    [Test]
    public async Task Handle_WhenPaymentAlreadyRecorded_ThrowsInvalidOperationException()
    {
        var appointment = SeedAppointment(AppointmentStatus.Completed);
        var handler = new RecordPaymentHandler(_db, new FakeTenantProvider(_tenantId));

        // First payment succeeds
        await handler.Handle(new RecordPaymentCommand(appointment.Id, 500m, PaymentMethod.Cash), CancellationToken.None);

        // Second payment for the same appointment should fail
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(new RecordPaymentCommand(appointment.Id, 300m, PaymentMethod.Cash), CancellationToken.None));
    }
}