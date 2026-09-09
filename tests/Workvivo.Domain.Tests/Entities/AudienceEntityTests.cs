using Shouldly;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Feed;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

/// <summary>
/// The audience key is what the feed query filters on. If it ever disagrees with the
/// type and target it was derived from, the row matches the wrong viewers and nothing
/// fails loudly - a post meant for one department quietly reaches the whole company.
/// These tests exist because that failure is silent.
/// </summary>
public class AudienceEntityTests
{
    [Fact]
    public void AllEmployees_uses_the_wildcard_key_and_carries_no_target()
    {
        var audience = new PostAudience(Guid.NewGuid(), AudienceType.AllEmployees, null);

        audience.Audience_Key.ShouldBe(AudienceKey.All);
        audience.Target_Id.ShouldBeNull();
    }

    [Theory]
    [InlineData(AudienceType.Department, "DEPT:")]
    [InlineData(AudienceType.Team, "TEAM:")]
    [InlineData(AudienceType.Location, "LOC:")]
    [InlineData(AudienceType.JobTitle, "JOB:")]
    [InlineData(AudienceType.Role, "ROLE:")]
    [InlineData(AudienceType.Community, "COMM:")]
    [InlineData(AudienceType.Employee, "EMP:")]
    public void Targeted_types_prefix_the_key_with_their_dimension(AudienceType type, string expectedPrefix)
    {
        var targetId = Guid.NewGuid();

        var audience = new PostAudience(Guid.NewGuid(), type, targetId);

        audience.Audience_Key.ShouldBe(expectedPrefix + targetId.ToString("D"));
    }

    [Fact]
    public void Two_dimensions_sharing_a_target_id_do_not_collide()
    {
        // A department and a team could be given the same Guid by coincidence or by a
        // bad import. Without the prefix they would produce the same key, and a post
        // for one would reach the other.
        var sharedId = Guid.NewGuid();

        var department = new PostAudience(Guid.NewGuid(), AudienceType.Department, sharedId);
        var team = new PostAudience(Guid.NewGuid(), AudienceType.Team, sharedId);

        department.Audience_Key.ShouldNotBe(team.Audience_Key);
    }

    [Fact]
    public void AllEmployees_rejects_a_target_id()
    {
        // Accepting one would mean a row claiming to target everyone while also naming
        // a department - two readings, and the key can only encode one.
        Should.Throw<ArgumentException>(
            () => new PostAudience(Guid.NewGuid(), AudienceType.AllEmployees, Guid.NewGuid()));
    }

    [Theory]
    [InlineData(AudienceType.Department)]
    [InlineData(AudienceType.Employee)]
    public void A_targeted_type_requires_a_target_id(AudienceType type)
    {
        Should.Throw<ArgumentException>(() => new PostAudience(Guid.NewGuid(), type, null));
    }

    [Fact]
    public void An_empty_guid_is_not_accepted_as_a_target()
    {
        // Guid.Empty is what an unset field deserialises to, so it is the value most
        // likely to arrive by accident. It would build a syntactically valid key that
        // matches nothing.
        Should.Throw<ArgumentException>(
            () => new PostAudience(Guid.NewGuid(), AudienceType.Department, Guid.Empty));
    }

    [Fact]
    public void Retargeting_rewrites_the_key_with_the_type_and_target()
    {
        var audience = new PostAudience(Guid.NewGuid(), AudienceType.Department, Guid.NewGuid());
        var newTarget = Guid.NewGuid();

        audience.Retarget(AudienceType.Location, newTarget);

        audience.Audience_Type.ShouldBe(AudienceType.Location);
        audience.Target_Id.ShouldBe(newTarget);
        audience.Audience_Key.ShouldBe("LOC:" + newTarget.ToString("D"));
    }

    [Fact]
    public void A_rejected_retarget_leaves_the_row_as_it_was()
    {
        // Half-applying the change would be the worst outcome: a type that no longer
        // matches its key.
        var originalTarget = Guid.NewGuid();
        var audience = new PostAudience(Guid.NewGuid(), AudienceType.Department, originalTarget);

        Should.Throw<ArgumentException>(() => audience.Retarget(AudienceType.Team, null));

        audience.Audience_Type.ShouldBe(AudienceType.Department);
        audience.Target_Id.ShouldBe(originalTarget);
        audience.Audience_Key.ShouldBe("DEPT:" + originalTarget.ToString("D"));
    }

    [Fact]
    public void Every_audience_type_has_a_key_format()
    {
        // A new enum member with no case in AudienceKey.For would throw at runtime the
        // first time somebody targeted it.
        foreach (var type in Enum.GetValues<AudienceType>())
        {
            Guid? target = type == AudienceType.AllEmployees ? null : Guid.NewGuid();

            var key = AudienceKey.For(type, target);

            key.ShouldNotBeNullOrWhiteSpace();
        }
    }
}
