using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Audit;

/// <summary>
/// An append-only record of a change worth accounting for.
///
/// Distinct from the template's <c>Security_AccessLogs</c>, which records which screen
/// somebody opened. This records what they altered, with the before and after, so a
/// question months later - who deactivated this account, who granted that permission -
/// has an answer.
///
/// Never updated and never deleted by application code. A mutable audit trail is not
/// an audit trail; retention is enforced by a scheduled archival job, not by handlers.
/// </summary>
public class AuditLog : BaseCommonEntity<long>
{
    /// <summary>Null for an anonymous action such as a failed sign-in attempt.</summary>
    public Guid? User_Id { get; set; }

    /// <summary>
    /// Username captured at the time.
    ///
    /// Denormalised deliberately: joining to the user table would show today's name,
    /// and would show nothing at all once the account is removed - exactly when the
    /// audit row matters most.
    /// </summary>
    public string? Username { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>CLR type name of what changed, for example <c>Post</c>.</summary>
    public string? Entity_Name { get; set; }

    /// <summary>Primary key of the affected row, as text so any key type fits.</summary>
    public string? Entity_Id { get; set; }

    /// <summary>
    /// JSON of the changed columns before and after.
    ///
    /// Only the columns that actually changed, and never a password hash, token or
    /// security stamp - an audit trail that copies secrets turns one leak into two.
    /// </summary>
    public string? Old_Values { get; set; }
    public string? New_Values { get; set; }

    /// <summary>Comma-separated list of changed column names, for cheap filtering.</summary>
    public string? Affected_Columns { get; set; }

    public string? Ip_Address { get; set; }
    public string? User_Agent { get; set; }

    /// <summary>Ties this row to the request that caused it, and to its log lines.</summary>
    public string? Correlation_Id { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual ApplicationUser? User { get; set; }
}
