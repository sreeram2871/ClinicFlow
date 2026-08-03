using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Domain.Enums;
using ClinicFlow.Api.Features.Auth;
using ClinicFlow.Api.Infrastructure.Auth;
using ClinicFlow.Api.Infrastructure.Data;
using ClinicFlow.Api.Tests.Features.Appointments;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace ClinicFlow.Api.Tests.Features.Auth;

public class LoginHandlerTests
{
    private ClinicFlowDbContext _db = null!;
    private Guid _tenantId;
    private const string ValidPassword = "Password123!";
    private const string StoredHash = "fake-hash-for-testing";

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

    private User SeedUser(bool isActive = true)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            FullName = "Test User",
            Email = "test@example.com",
            PasswordHash = StoredHash,
            Role = UserRole.Admin,
            IsActive = isActive
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    [Test]
    public async Task Handle_WithCorrectCredentials_ReturnsTokenAndUserInfo()
    {
        var user = SeedUser();
        var handler = new LoginHandler(_db, new FakePasswordHasher(shouldVerifySucceed: true), new FakeJwtTokenService(), new FakeRefreshTokenService());

        var result = await handler.Handle(new LoginCommand(user.Email, ValidPassword), CancellationToken.None);

        Assert.That(result.AccessToken, Is.EqualTo("fake-token"));
        Assert.That(result.FullName, Is.EqualTo("Test User"));
        Assert.That(result.Role, Is.EqualTo("Admin"));
    }

    [Test]
    public void Handle_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        var user = SeedUser();
        var handler = new LoginHandler(_db, new FakePasswordHasher(shouldVerifySucceed: false), new FakeJwtTokenService(), new FakeRefreshTokenService());
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handle(new LoginCommand(user.Email, "WrongPassword"), CancellationToken.None));
    }
    [Test]
    public void Handle_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
    {
        var handler = new LoginHandler(_db, new FakePasswordHasher(shouldVerifySucceed: true), new FakeJwtTokenService(), new FakeRefreshTokenService());

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handle(new LoginCommand("doesnotexist@example.com", ValidPassword), CancellationToken.None));
    }

    [Test]
    public void Handle_WithDeactivatedAccount_ThrowsUnauthorizedAccessException()
    {
        var user = SeedUser(isActive: false);
        var handler = new LoginHandler(_db, new FakePasswordHasher(shouldVerifySucceed: true), new FakeJwtTokenService(), new FakeRefreshTokenService());

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handle(new LoginCommand(user.Email, ValidPassword), CancellationToken.None));
    }
}

/// <summary>Fake password hasher — controllable Verify() result, no real BCrypt work needed for these tests.</summary>
public class FakePasswordHasher : IPasswordHasher
{
    private readonly bool _shouldVerifySucceed;
    public FakePasswordHasher(bool shouldVerifySucceed) => _shouldVerifySucceed = shouldVerifySucceed;

    public string Hash(string plainTextPassword) => "fake-hash";
    public bool Verify(string plainTextPassword, string hash) => _shouldVerifySucceed;
}

/// <summary>Fake JWT service — returns a fixed token, no real cryptographic signing needed for these tests.</summary>
public class FakeJwtTokenService : IJwtTokenService
{
    public string GenerateAccessToken(User user) => "fake-token";
}
public class FakeRefreshTokenService : IRefreshTokenService
{
    public Task<string> GenerateAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult("fake-refresh-token");

    public Task<User?> ValidateAndConsumeAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult<User?>(null);

    public Task RevokeAsync(string token, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}