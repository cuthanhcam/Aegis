namespace Aegis.Application.Contracts;

public sealed class PreconditionRequiredException : Exception
{
    public PreconditionRequiredException(string message) : base(message)
    {
    }
}
