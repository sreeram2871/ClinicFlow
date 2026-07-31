using ClinicFlow.Api.Domain.Entities;

namespace ClinicFlow.Api.Infrastructure.Auth;

/// <summary>
/// Generates signed JWT access tokens for authenticated users. Kept behind
/// an interface for the same reason as IPasswordHasher — swappable and
/// testable without depending on a concrete token library everywhere.
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
}