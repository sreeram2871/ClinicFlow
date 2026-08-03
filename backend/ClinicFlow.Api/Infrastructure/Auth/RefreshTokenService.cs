using System.Security.Cryptography;
using ClinicFlow.Api.Domain.Entities;
using ClinicFlow.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Infrastructure.Auth;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ClinicFlowDbContext _db;
    private const int ExpiryDays = 7;

    public RefreshTokenService(ClinicFlowDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync(User user, CancellationToken cancellationToken)
    {
        var tokenValue = GenerateSecureRandomToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            UserId = user.Id,
            Token = tokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(ExpiryDays),
            IsRevoked = false
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return tokenValue;
    }

    public async Task<User?> ValidateAndConsumeAsync(string token, CancellationToken cancellationToken)
    {
        var refreshToken = await _db.RefreshTokens
            .IgnoreQueryFilters() // no tenant context yet — this IS how we establish it
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);

        if (refreshToken is null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        // Rotate: revoke the used token, caller is expected to generate
        // a fresh one — a refresh token should only ever be usable once.
        refreshToken.IsRevoked = true;
        await _db.SaveChangesAsync(cancellationToken);

        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == refreshToken.UserId, cancellationToken);

        return user is { IsActive: true } ? user : null;
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken)
    {
        var refreshToken = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);

        if (refreshToken is not null)
        {
            refreshToken.IsRevoked = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GenerateSecureRandomToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}