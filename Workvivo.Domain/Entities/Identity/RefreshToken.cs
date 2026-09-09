using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Identity;

/// <summary>
/// A refresh token, stored hashed, rotated on every use.
///
/// Three properties matter, and each is a column here:
///
///   The raw token is never persisted. Only a SHA-256 of it is, so a database leak
///   does not hand over usable sessions - the same reasoning as password hashing,
///   without the need for a slow KDF because the token is already high-entropy.
///
///   Every use issues a new token and revokes this one, so a stolen token is only
///   good until the legitimate holder next refreshes.
///
///   All tokens descended from one sign-in share a <see cref="Family_Id"/>. If an
///   already-revoked token is presented, that means two parties hold the same
///   token - a replay - and the whole family is revoked at once, ending the attacker's
///   session and the victim's together. Rotation alone cannot detect this; the family
///   is what makes it visible.
/// </summary>
public class RefreshToken : BaseCommonEntity<Guid>
{
    public Guid User_Id { get; set; }

    /// <summary>SHA-256 of the raw token, lowercase hex. Unique.</summary>
    public string Token_Hash { get; set; } = string.Empty;

    /// <summary>Shared by every token descended from one sign-in.</summary>
    public Guid Family_Id { get; set; }

    public DateTime Created_At { get; set; }
    public DateTime Expires_At { get; set; }

    public DateTime? Revoked_At { get; set; }
    public string? Revoked_Reason { get; set; }

    /// <summary>The token issued when this one was rotated out.</summary>
    public Guid? Replaced_By_Id { get; set; }

    public string? Created_Ip { get; set; }
    public string? Created_User_Agent { get; set; }
    public string? Revoked_Ip { get; set; }

    public virtual ApplicationUser? User { get; set; }
    public virtual RefreshToken? ReplacedBy { get; set; }

    public bool IsActiveAt(DateTime utcNow) => Revoked_At is null && Expires_At > utcNow;
}
