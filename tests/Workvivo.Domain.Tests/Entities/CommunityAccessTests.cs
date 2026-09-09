using Shouldly;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.Communities;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

public class CommunityAccessTests
{
    [Fact]
    public void Only_public_communities_are_readable_by_non_members()
    {
        new Community { Privacy = CommunityPrivacy.Public }.AllowsPublicReading.ShouldBeTrue();
        new Community { Privacy = CommunityPrivacy.Private }.AllowsPublicReading.ShouldBeFalse();

        // Restricted communities are listed and described, but their content is not
        // open - which is exactly the case an "is it private?" check gets wrong.
        new Community { Privacy = CommunityPrivacy.Restricted }.AllowsPublicReading.ShouldBeFalse();
    }

    [Theory]
    [InlineData(CommunityPrivacy.Public, false)]
    [InlineData(CommunityPrivacy.Private, true)]
    [InlineData(CommunityPrivacy.Restricted, true)]
    public void Approval_is_required_to_join_anything_but_a_public_community(
        CommunityPrivacy privacy, bool expected)
    {
        new Community { Privacy = privacy }.RequiresApprovalToJoin.ShouldBe(expected);
    }

    [Theory]
    [InlineData(MembershipStatus.Pending)]
    [InlineData(MembershipStatus.Rejected)]
    [InlineData(MembershipStatus.Banned)]
    [InlineData(MembershipStatus.Left)]
    public void Only_an_approved_membership_grants_access(MembershipStatus status)
    {
        new CommunityMember { Membership_Status = status }.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void A_soft_deleted_membership_grants_nothing()
    {
        var member = new CommunityMember
        {
            Membership_Status = MembershipStatus.Approved,
            Member_Role = CommunityMemberRole.Owner,
            Is_Deleted = true,
        };

        member.IsActive.ShouldBeFalse();
        member.CanModerate.ShouldBeFalse();
    }

    [Fact]
    public void A_moderator_whose_membership_is_not_approved_cannot_moderate()
    {
        // Role and status are separate axes on purpose. A pending moderator is not yet
        // a moderator of anything, and checking the role alone would miss that.
        new CommunityMember
        {
            Member_Role = CommunityMemberRole.Moderator,
            Membership_Status = MembershipStatus.Pending,
        }.CanModerate.ShouldBeFalse();
    }

    [Theory]
    [InlineData(CommunityMemberRole.Moderator, true)]
    [InlineData(CommunityMemberRole.Owner, true)]
    [InlineData(CommunityMemberRole.Member, false)]
    public void Moderation_follows_the_role_for_an_approved_member(
        CommunityMemberRole role, bool expected)
    {
        new CommunityMember
        {
            Member_Role = role,
            Membership_Status = MembershipStatus.Approved,
        }.CanModerate.ShouldBe(expected);
    }
}
