namespace Workvivo.Domain.Exceptions;

/// <summary>
/// Base type for every failure the application raises deliberately.
///
/// The point of the hierarchy is that <c>ProblemDetailsFactory</c> can map an
/// exception to a status code without a chain of <c>is</c> checks that has to be
/// edited every time a new failure mode appears: the exception carries its own
/// status and error slug. Anything that does <em>not</em> derive from this is by
/// definition unexpected, and is logged as an error and returned as a bare 500.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    /// <summary>HTTP status this failure maps to.</summary>
    public abstract int StatusCode { get; }

    /// <summary>
    /// Stable slug used to build the RFC 7807 <c>type</c> URI, e.g. <c>not-found</c>.
    /// Clients may branch on it; the human-readable message may be translated freely.
    /// </summary>
    public abstract string ErrorType { get; }

    /// <summary>
    /// Optional machine-readable payload merged into the ProblemDetails extensions.
    /// Used by <see cref="ValidationException"/> for per-field errors.
    /// </summary>
    public virtual IReadOnlyDictionary<string, object?>? Extensions => null;
}
