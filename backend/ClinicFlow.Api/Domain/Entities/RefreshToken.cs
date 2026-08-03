namespace ClinicFlow.Api.Domain.Entities;

/// <summary>
/// A long-lived token allowing a client to obtain new access tokens
/// without re-entering credentials. Stored server-side so tokens can be
/// individually revoked (e.g. on logout) — unlike access tokens, which
/// are stateless and can't be invalidated before they naturally expire.
/// </summary>
public class RefreshToken : TenantScopedEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}