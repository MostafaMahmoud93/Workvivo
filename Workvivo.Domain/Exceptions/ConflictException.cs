namespace Workvivo.Domain.Exceptions;

/// <summary>
/// The request cannot be applied to the current state of the resource - a duplicate
/// key, or an optimistic-concurrency clash where somebody else saved first.
/// </summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public override int StatusCode => StatusCodes.Status409Conflict;

    public override string ErrorType => "conflict";
}
