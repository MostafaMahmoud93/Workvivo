using Shouldly;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Organization;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

/// <summary>
/// The audience key is the join between "who is this for" and "what should this person
/// see". These pin the two halves agreeing.
///
/// A mismatch here does not throw - it shows content to the wrong people, or hides it
/// from the right ones, with nothing in a log to say so.
/// </summary>
public class AudienceKeyCoverageTests
{
    private static readonly Guid TechnologyId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EngineeringId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PlatformId = new("33333333-3333-3333-3333-333333333333");

    /// <summary>
    /// Mirrors how AudienceResolver derives department keys: every id along the
    /// materialised path, not just the employee's own department.
    /// </summary>
    private static IReadOnlyList<string> DepartmentKeysFor(string path) =>
    [
        .. path
            .Split(DepartmentPath.Separator, StringSplitOptions.RemoveEmptyEntries)
            .Select(Guid.Parse)
            .Select(id => AudienceKey.For(AudienceType.Department, id)),
    ];

    [Fact]
    public void A_post_targeted_at_a_division_reaches_someone_several_levels_below_it()
    {
        // The rule that is easy to miss. An announcement aimed at Technology has to
        // reach an engineer in Platform, three levels down - otherwise a division-wide
        // message silently misses most of the division.
        var technology = DepartmentPath.Build(null, TechnologyId);
        var engineering = DepartmentPath.Build(technology, EngineeringId);
        var platform = DepartmentPath.Build(engineering, PlatformId);

        var viewerKeys = DepartmentKeysFor(platform);
        var postKey = AudienceKey.For(AudienceType.Department, TechnologyId);

        viewerKeys.ShouldContain(postKey);
    }

    [Fact]
    public void A_post_targeted_at_a_sub_department_does_not_reach_someone_above_it()
    {
        // Targeting is downward only. A note for Platform must not appear on the
        // division head's feed simply because they sit above it.
        var technology = DepartmentPath.Build(null, TechnologyId);

        var viewerKeys = DepartmentKeysFor(technology);
        var postKey = AudienceKey.For(AudienceType.Department, PlatformId);

        viewerKeys.ShouldNotContain(postKey);
    }

    [Fact]
    public void A_post_targeted_at_a_sibling_department_does_not_reach_across()
    {
        var technology = DepartmentPath.Build(null, TechnologyId);
        var engineering = DepartmentPath.Build(technology, EngineeringId);

        var salesId = new Guid("44444444-4444-4444-4444-444444444444");
        var sales = DepartmentPath.Build(null, salesId);

        DepartmentKeysFor(engineering)
            .ShouldNotContain(AudienceKey.For(AudienceType.Department, salesId));

        DepartmentKeysFor(sales)
            .ShouldNotContain(AudienceKey.For(AudienceType.Department, EngineeringId));
    }

    [Fact]
    public void An_employee_targeted_post_matches_only_that_employee()
    {
        var recipient = Guid.NewGuid();
        var someoneElse = Guid.NewGuid();

        AudienceKey.For(AudienceType.Employee, recipient)
            .ShouldNotBe(AudienceKey.For(AudienceType.Employee, someoneElse));
    }

    [Fact]
    public void The_wildcard_key_is_a_constant_every_viewer_carries()
    {
        // Every key set includes ALL, so a post addressed to everyone needs exactly one
        // audience row rather than one per employee - which is what makes a company-wide
        // announcement cheap at a hundred thousand staff.
        AudienceKey.For(AudienceType.AllEmployees, null).ShouldBe(AudienceKey.All);
    }

    [Fact]
    public void Keys_from_different_dimensions_never_collide()
    {
        // A department and a team could be handed the same Guid by a bad import. Without
        // the dimension prefix their keys would be equal and a post for one would reach
        // the other.
        var shared = Guid.NewGuid();

        var keys = new[]
        {
            AudienceKey.For(AudienceType.Department, shared),
            AudienceKey.For(AudienceType.Team, shared),
            AudienceKey.For(AudienceType.Location, shared),
            AudienceKey.For(AudienceType.JobTitle, shared),
            AudienceKey.For(AudienceType.Role, shared),
            AudienceKey.For(AudienceType.Community, shared),
            AudienceKey.For(AudienceType.Employee, shared),
        };

        keys.Distinct().Count().ShouldBe(keys.Length);
    }

    [Fact]
    public void Every_key_fits_the_column_it_is_stored_in()
    {
        // Audience_Key is nvarchar(64) and is the index the whole feed seeks on. A key
        // that overflowed would be truncated and match the wrong rows.
        foreach (var type in Enum.GetValues<AudienceType>())
        {
            Guid? target = type == AudienceType.AllEmployees ? null : Guid.NewGuid();

            AudienceKey.For(type, target).Length.ShouldBeLessThanOrEqualTo(64);
        }
    }
}
