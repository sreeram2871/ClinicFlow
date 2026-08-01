using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Features.Patients.Shared;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Tests.Features.Appointments;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace ClinicFlow.Api.Tests.Features.Patients;

public class PatientAccessGuardTests
{
    private ClinicFlowDbContext _db = null!;
    private Guid _tenantId;
    private Guid _patientId;
    private Guid _patientOwnUserId;
    private Guid _treatingDoctorId;
    private Guid _otherDoctorId;

    [SetUp]
    public async Task Setup()
    {
        _tenantId = Guid.NewGuid();
        _patientId = Guid.NewGuid();
        _patientOwnUserId = Guid.NewGuid();
        _treatingDoctorId = Guid.NewGuid();
        _otherDoctorId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<ClinicFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ClinicFlowDbContext(options, new FakeTenantProvider(_tenantId));

        _db.Patients.Add(new Patient
        {
            Id = _patientId,
            TenantId = _tenantId,
            UserId = _patientOwnUserId,
            FullName = "Test Patient",
            DateOfBirth = new DateTime(1990, 1, 1)
        });

        // The treating doctor has an appointment with this patient; the other doctor doesn't.
        _db.Appointments.Add(new Appointment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PatientId = _patientId,
            DoctorId = _treatingDoctorId,
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            ScheduledEnd = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = AppointmentStatus.Confirmed
        });

        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void EnforceAsync_AdminRole_AlwaysAllowed()
    {
        var guard = new PatientAccessGuard(_db);
        Assert.DoesNotThrowAsync(async () =>
            await guard.EnforceAsync(_patientId, _patientOwnUserId, Guid.NewGuid(), "Admin", CancellationToken.None));
    }

    [Test]
    public void EnforceAsync_ReceptionistRole_AlwaysAllowed()
    {
        var guard = new PatientAccessGuard(_db);
        Assert.DoesNotThrowAsync(async () =>
            await guard.EnforceAsync(_patientId, _patientOwnUserId, Guid.NewGuid(), "Receptionist", CancellationToken.None));
    }

    [Test]
    public void EnforceAsync_PatientAccessingOwnRecord_Allowed()
    {
        var guard = new PatientAccessGuard(_db);
        Assert.DoesNotThrowAsync(async () =>
            await guard.EnforceAsync(_patientId, _patientOwnUserId, _patientOwnUserId, "Patient", CancellationToken.None));
    }

    [Test]
    public void EnforceAsync_PatientAccessingSomeoneElsesRecord_ThrowsForbidden()
    {
        var guard = new PatientAccessGuard(_db);
        var differentUserId = Guid.NewGuid();

        Assert.ThrowsAsync<ClinicFlow.Api.Common.Errors.ForbiddenException>(async () =>
            await guard.EnforceAsync(_patientId, _patientOwnUserId, differentUserId, "Patient", CancellationToken.None));
    }

    [Test]
    public void EnforceAsync_DoctorWhoTreatedPatient_Allowed()
    {
        var guard = new PatientAccessGuard(_db);
        Assert.DoesNotThrowAsync(async () =>
            await guard.EnforceAsync(_patientId, _patientOwnUserId, _treatingDoctorId, "Doctor", CancellationToken.None));
    }

    [Test]
    public void EnforceAsync_DoctorWhoNeverTreatedPatient_ThrowsForbidden()
    {
        var guard = new PatientAccessGuard(_db);
        Assert.ThrowsAsync<ClinicFlow.Api.Common.Errors.ForbiddenException>(async () =>
            await guard.EnforceAsync(_patientId, _patientOwnUserId, _otherDoctorId, "Doctor", CancellationToken.None));
    }
}