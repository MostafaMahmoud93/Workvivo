using Shouldly;
using Workvivo.Application.Features.Communities.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.Communities;
using Xunit;

namespace Workvivo.Application.Tests.Features.Communities;

/// <summary>
/// Communities are the first place in the product where visibility is not one
/// permission. Private must be invisible, Restricted must be listed but unreadable,
/// and a moderator's powers come from a membership row rather than a role.
///
/// Every combination below is a decision the UI and three handlers depend on, and
/// getting one wrong either leaks a private group or locks its owner out.
/// </summary>
public class CommunityAccessTests
{
    private static readonly Guid Me = Guid.NewGuid();

    private static Community Community(CommunityPrivacy privacy, bool active = true) => new()
    {
        Id = Guid.NewGuid(),
        Privacy = privacy,
        Is_Active = active,
        Owner_Employee_Id = Guid.NewGuid(),
    };

    private static CommunityMember Membership(
        MembershipStatus status = MembershipStatus.Approved,
        CommunityMemberRole role = CommunityMemberRole.Member) => new()
    {
        Employee_Id = Me,
        Membership_Status = status,
        Member_Role = role,
    };

    private static CommunityAccess Access(
        Community community,
        CommunityMember? membership = null,
        bool platformManager = false) =>
        new(community, membership, Me, platformManager);

    [Fact]
    public void A_public_community_is_readable_by_anybody()
    {
        Access(Community(CommunityPrivacy.Public)).CanRead.ShouldBeTrue();
    }

    [Theory]
    [InlineData(CommunityPrivacy.Private)]
    [InlineData(CommunityPrivacy.Restricted)]
    public void A_closed_community_is_not_readable_by_a_non_member(CommunityPrivacy privacy)
    {
        Access(Community(privacy)).CanRead.ShouldBeFalse();
    }

    [Fact]
    public void A_restricted_community_is_still_discoverable()
    {
        // The distinction that is easy to collapse: Restricted is listed and described
        // so somebody can decide to ask to join. Only Private is hidden outright.
        var access = Access(Community(CommunityPrivacy.Restricted));

        access.CanRead.ShouldBeFalse();
        access.CanDiscover.ShouldBeTrue();
    }

    [Fact]
    public void A_private_community_is_not_discoverable_by_a_non_member()
    {
        Access(Community(CommunityPrivacy.Private)).CanDiscover.ShouldBeFalse();
    }

    [Fact]
    public void A_private_community_is_readable_by_its_members()
    {
        Access(Community(CommunityPrivacy.Private), Membership()).CanRead.ShouldBeTrue();
    }

    [Theory]
    [InlineData(MembershipStatus.Pending)]
    [InlineData(MembershipStatus.Rejected)]
    [InlineData(MembershipStatus.Banned)]
    [InlineData(MembershipStatus.Left)]
    public void Only_an_approved_membership_opens_a_private_community(MembershipStatus status)
    {
        Access(Community(CommunityPrivacy.Private), Membership(status)).CanRead.ShouldBeFalse();
    }

    [Fact]
    public void A_platform_manager_can_read_and_moderate_a_community_they_are_not_in()
    {
        var access = Access(Community(CommunityPrivacy.Private), platformManager: true);

        access.CanRead.ShouldBeTrue();
        access.CanModerate.ShouldBeTrue();
        access.CanManage.ShouldBeTrue();
    }

    [Fact]
    public void Reading_is_not_posting()
    {
        // A public community is readable by everybody and postable only by members.
        // Conflating the two turns every group into an open wall.
        var access = Access(Community(CommunityPrivacy.Public));

        access.CanRead.ShouldBeTrue();
        access.CanPost.ShouldBeFalse();
    }

    [Fact]
    public void A_plain_member_cannot_moderate()
    {
        Access(Community(CommunityPrivacy.Public), Membership()).CanModerate.ShouldBeFalse();
    }

    [Fact]
    public void A_moderator_moderates_but_does_not_own()
    {
        // The separation that stops a moderator appointing accomplices: changing
        // settings and roles is the owner's alone.
        var access = Access(
            Community(CommunityPrivacy.Public),
            Membership(role: CommunityMemberRole.Moderator));

        access.CanModerate.ShouldBeTrue();
        access.CanManage.ShouldBeFalse();
    }

    [Fact]
    public void A_pending_moderator_moderates_nothing()
    {
        // Role and status are separate axes. A moderator whose membership has not been
        // approved is not yet a moderator of anything.
        var access = Access(
            Community(CommunityPrivacy.Public),
            Membership(MembershipStatus.Pending, CommunityMemberRole.Moderator));

        access.CanModerate.ShouldBeFalse();
    }

    [Fact]
    public void The_owner_can_manage()
    {
        Access(Community(CommunityPrivacy.Public), Membership(role: CommunityMemberRole.Owner))
            .CanManage.ShouldBeTrue();
    }

    [Fact]
    public void A_banned_person_is_offered_no_way_back_in()
    {
        Access(Community(CommunityPrivacy.Public), Membership(MembershipStatus.Banned))
            .CanRequestToJoin.ShouldBeFalse();
    }

    [Theory]
    [InlineData(MembershipStatus.Left)]
    [InlineData(MembershipStatus.Rejected)]
    public void Somebody_who_left_or_was_turned_down_may_ask_again(MembershipStatus status)
    {
        Access(Community(CommunityPrivacy.Public), Membership(status))
            .CanRequestToJoin.ShouldBeTrue();
    }

    [Fact]
    public void An_existing_member_is_not_offered_a_join_button()
    {
        Access(Community(CommunityPrivacy.Public), Membership()).CanRequestToJoin.ShouldBeFalse();
    }

    [Fact]
    public void A_closed_community_cannot_be_joined()
    {
        Access(Community(CommunityPrivacy.Public, active: false)).CanRequestToJoin.ShouldBeFalse();
    }

    [Fact]
    public void A_deleted_community_is_readable_by_nobody_including_a_platform_manager()
    {
        var community = Community(CommunityPrivacy.Public);
        community.Is_Deleted = true;

        Access(community, platformManager: true).CanRead.ShouldBeFalse();
    }
}
