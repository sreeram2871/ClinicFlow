namespace ClinicFlow.Api.Infrastructure.Auth;

/// <summary>
/// BCrypt-based implementation. BCrypt is deliberately slow (by design,
/// via its "work factor") — this makes brute-forcing a stolen password
/// database computationally expensive, unlike fast hashes (MD5, SHA256)
/// which are wrong for passwords precisely because they're too fast.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plainTextPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
    }

    public bool Verify(string plainTextPassword, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(plainTextPassword, hash);
    }
}