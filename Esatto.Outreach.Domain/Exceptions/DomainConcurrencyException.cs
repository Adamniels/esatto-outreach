namespace Esatto.Outreach.Domain.Exceptions;

public class DomainConcurrencyException : Exception
{
    public DomainConcurrencyException(string message) : base(message)
    {
    }

    public DomainConcurrencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
