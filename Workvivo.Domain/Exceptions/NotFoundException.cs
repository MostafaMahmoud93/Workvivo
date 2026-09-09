namespace Workvivo.Domain.Exceptions;

/// <summary>
/// The requested resource does not exist, or the caller may not know that it does.
///
/// Deliberately also used where 403 would leak information: answering "forbidden"
/// for a record id tells an attacker the id is real. Use <see cref="ForbiddenException"/>
/// only when the caller already legitimately knows the resource exists.
/// </summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }

    public string? EntityName { get; }

    public object? Key { get; }

    public override int StatusCode => StatusCodes.Status404NotFound;

    public override string ErrorType => "not-found";
}
