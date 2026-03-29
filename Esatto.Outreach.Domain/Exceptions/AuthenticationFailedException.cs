namespace Esatto.Outreach.Domain.Exceptions;

/// <summary>
/// Thrown when authentication fails due to invalid credentials, expired tokens, or invalid invitations.
/// </summary>
public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message) { }

    public AuthenticationFailedException(string message, Exception innerException) : base(message, innerException) { }
}
