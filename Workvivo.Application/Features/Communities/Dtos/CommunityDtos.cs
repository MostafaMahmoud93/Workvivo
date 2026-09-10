namespace Workvivo.Application.Features.Communities.Dtos;

/// <summary>A community as it appears in a list.</summary>
public class CommunitySummaryDto
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Privacy { get; init; }
    public Guid? LogoFileId { get; init; }
    public int MembersCount { get; init; }
    public int PostsCount { get; init; }
    public bool IsFeatured { get; init; }

    /// <summary>The caller's standing, so the list can show Join, Pending or Open.</summary>
    public int? MyMembershipStatus { get; init; }
    public int? MyRole { get; init; }
    public bool CanJoin { get; init; }
}

/// <summary>Everything the community page needs, in one response.</summary>
public sealed class CommunityDetailDto : CommunitySummaryDto
{
    public Guid? CoverFileId { get; init; }
    public Guid OwnerEmployeeId { get; init; }
    public string? OwnerDisplayName { get; init; }
    public bool IsActive { get; init; }

    /// <summary>What the caller may do, so the client does not guess.</summary>
    public bool CanRead { get; init; }
    public bool CanPost { get; init; }
    public bool CanModerate { get; init; }
    public bool CanManage { get; init; }

    /// <summary>Only populated for moderators - the size of the approval queue.</summary>
    public int PendingRequestsCount { get; init; }
}

public sealed class CommunityMemberDto
{
    public Guid EmployeeId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? JobTitle { get; init; }
    public Guid? ProfilePictureFileId { get; init; }
    public int Role { get; init; }
    public int Status { get; init; }
    public DateTime? JoinedAt { get; init; }
    public DateTime RequestedAt { get; init; }
}

public sealed class CommunityInvitationDto
{
    public Guid Id { get; init; }
    public Guid CommunityId { get; init; }
    public string CommunityName { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string InvitedByDisplayName { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
