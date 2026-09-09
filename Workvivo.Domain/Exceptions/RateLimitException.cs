namespace Workvivo.Domain.Exceptions;

/// <summary>
/// The caller exceeded a quota. Raised by application-level throttles (for example
/// "no more than N recognitions per day"); transport-level throttling is handled by
/// the ASP.NET Core rate limiter middleware, which answers 429 on its own.
/// </summary>
public sealed class RateLimitException : AppException
{
    public RateLimitException(string message, TimeSpan? retryAfter = null)
        : base(message)
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan? RetryAfter { get; }

    public override int StatusCode => StatusCodes.Status429TooManyRequests;

    public override string ErrorType => "rate-limit";

    public override IReadOnlyDictionary<string, object?>? Extensions =>
        RetryAfter is null
            ? null
            : new Dictionary<string, object?> { ["retryAfterSeconds"] = (int)RetryAfter.Value.TotalSeconds };
}
