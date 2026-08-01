using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Features.Auth;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Tests.Features.Appointments;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace ClinicFlow.Api.Tests.Features.Auth;

public class RegisterPatientHandlerTests
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

    [Test]
    public async Task Handle_WithNewEmail_CreatesUserAndPatientSuccessfully()
    {
        var handler = new RegisterPatientHandler(_db, new FakePasswordHasher(shouldVerifySucceed: true));

        var command = new RegisterPatientCommand(
            _tenantId, "New Patient", "new@example.com", "Password123!", "9999999999", new DateTime(1995, 1, 1));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(await _db.Users.CountAsync(), Is.EqualTo(1));
        Assert.That(await _db.Patients.CountAsync(), Is.EqualTo(1));

        var createdUser = await _db.Users.FindAsync(result.UserId);
        Assert.That(createdUser!.Role, Is.EqualTo(UserRole.Patient));

        var createdPatient = await _db.Patients.FindAsync(result.PatientId);
        Assert.That(createdPatient!.UserId, Is.EqualTo(result.UserId));
    }

    [Test]
    public void Handle_WithAlreadyRegisteredEmail_ThrowsArgumentException()
    {
        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            FullName = "Existing User",
            Email = "taken@example.com",
            PasswordHash = "hash",
            Role = UserRole.Patient
        });
        _db.SaveChanges();

        var handler = new RegisterPatientHandler(_db, new FakePasswordHasher(shouldVerifySucceed: true));
        var command = new RegisterPatientCommand(
            _tenantId, "New Patient", "taken@example.com", "Password123!", "9999999999", new DateTime(1995, 1, 1));

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await handler.Handle(command, CancellationToken.None));
    }
}