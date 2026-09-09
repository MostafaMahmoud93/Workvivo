namespace Workvivo.Domain.Exceptions;

/// <summary>
/// No valid credentials were presented, or the ones presented have expired.
/// </summary>
public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Authentication is required.")
        : base(message)
    {
    }

    public override int StatusCode => StatusCodes.Status401Unauthorized;

    public override string ErrorType => "unauthorized";
}
