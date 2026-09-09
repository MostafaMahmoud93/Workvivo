using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Infrastructure.Common;

/// <summary>
/// The caller for work that has no caller - background jobs, scheduled publishing,
/// seeding.
///
/// It exists so those paths do not have to special-case a null ICurrentUser, and so
/// the audit trail records "system" rather than an empty user id or, worse, whichever
/// admin account happened to be handy.
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    /// <summary>Fixed id recorded as the actor for automated changes.</summary>
    public static readonly Guid SystemUserId = new("00000000-0000-0000-0000-00000000515e");

    private readonly string? _correlationId;

    public SystemCurrentUser(string? correlationId = null)
    {
        _correlationId = correlationId;
    }

    public Guid? UserId => SystemUserId;

    public string? UserName => "system";

    public bool IsAuthenticated => true;

    /// <summary>
    /// False on purpose. Background work should pass explicit permission checks like
    /// anything else; a job that needs elevation asks for it deliberately rather than
    /// inheriting a blanket admin flag.
    /// </summary>
    public bool IsAdmin => false;

    public string? UserTypeCode => "SYSTM";

    public string? IpAddress => null;

    public string? UserAgent => "Workvivo.BackgroundJobs";

    public string? CorrelationId => _correlationId;
}
