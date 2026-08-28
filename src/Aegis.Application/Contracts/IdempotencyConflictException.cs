namespace Aegis.Application.Contracts;

public sealed class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException(string message) : base(message)
    {
    }
}
