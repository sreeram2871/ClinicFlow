namespace ClinicFlow.Api.Common.Errors;

/// <summary>
/// Thrown when an authenticated user is correctly identified but not
/// permitted to access a specific resource — maps to 403 Forbidden.
/// Distinct from UnauthorizedAccessException (401), which means the
/// caller's identity itself couldn't be established or verified.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}