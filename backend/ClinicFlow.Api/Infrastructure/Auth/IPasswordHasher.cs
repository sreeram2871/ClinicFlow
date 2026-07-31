namespace ClinicFlow.Api.Infrastructure.Auth;

/// <summary>
/// Wraps password hashing so feature code never calls a hashing library
/// directly — keeps the algorithm swappable and easy to fake in tests.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string hash);
}