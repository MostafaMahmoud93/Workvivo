namespace Workvivo.Domain.Exceptions;

/// <summary>
/// The caller is authenticated but not allowed to perform this operation.
/// </summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message)
    {
    }

    public ForbiddenException(string message, string requiredPermission)
        : base(message)
    {
        RequiredPermission = requiredPermission;
    }

    /// <summary>
    /// Never returned to the client - it would map the permission model for an attacker.
    /// Logged server-side so an operator can see which check refused the call.
    /// </summary>
    public string? RequiredPermission { get; }

    public override int StatusCode => StatusCodes.Status403Forbidden;

    public override string ErrorType => "forbidden";
}
