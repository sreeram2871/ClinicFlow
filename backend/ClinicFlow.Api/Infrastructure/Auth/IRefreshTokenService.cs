using ClinicFlow.Api.Domain.Entities;

namespace ClinicFlow.Api.Infrastructure.Auth;

public interface IRefreshTokenService
{
    Task<string> GenerateAsync(User user, CancellationToken cancellationToken);
    Task<User?> ValidateAndConsumeAsync(string token, CancellationToken cancellationToken);
    Task RevokeAsync(string token, CancellationToken cancellationToken);
}