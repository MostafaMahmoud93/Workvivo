namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Everything about the caller that application code is allowed to depend on.
///
/// Extends what <see cref="IUserAccessor"/> already offered (id, application, user
/// type) with the request forensics that auditing needs and the permission set that
/// authorisation needs, so no handler ever reaches for IHttpContextAccessor itself.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? UserName { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }

    string? UserTypeCode { get; }

    /// <summary>Client address, honouring a trusted forwarded-for header. Audited, never trusted for authorisation.</summary>
    string? IpAddress { get; }

    string? UserAgent { get; }

    /// <summary>Per-request correlation id, echoed to the client and stamped on every log line.</summary>
    string? CorrelationId { get; }
}
