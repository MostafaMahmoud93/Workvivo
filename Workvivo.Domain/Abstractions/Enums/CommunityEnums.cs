namespace Workvivo.Domain.Abstractions.Enums;

/// <summary>
/// Who can see a community and how someone gets in.
/// </summary>
public enum CommunityPrivacy
{
    /// <summary>Listed, readable by everyone, anyone may join without approval.</summary>
    Public = 0,

    /// <summary>Not listed to non-members; content hidden; membership by invitation only.</summary>
    Private = 1,

    /// <summary>Listed and described, but content and joining require approval.</summary>
    Restricted = 2,
}

/// <summary>
/// A member's standing in a community.
///
/// Moderators are members with a role rather than rows in a separate table - a
/// moderator who is not a member is a state with no meaning here.
/// </summary>
public enum CommunityMemberRole
{
    Member = 0,
    Moderator = 1,
    Owner = 2,
}

public enum MembershipStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Banned = 3,
    Left = 4,
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3,
    Revoked = 4,
}
