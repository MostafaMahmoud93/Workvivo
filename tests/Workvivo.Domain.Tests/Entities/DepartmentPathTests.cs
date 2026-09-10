using Shouldly;
using Workvivo.Domain.Entities.Organization;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

/// <summary>
/// The materialised path is what makes "everything under this division" a single
/// indexed range scan. Every audience rule, departmental filter and org-chart query
/// reads it, so a wrong path does not throw - it quietly returns the wrong people.
/// </summary>
public class DepartmentPathTests
{
    private static readonly Guid Root = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Child = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Grandchild = new("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void A_root_path_is_wrapped_in_separators()
    {
        DepartmentPath.Build(null, Root).ShouldBe($"/{Root:D}/");
    }

    [Fact]
    public void A_child_path_extends_its_parent()
    {
        var rootPath = DepartmentPath.Build(null, Root);

        DepartmentPath.Build(rootPath, Child).ShouldBe($"/{Root:D}/{Child:D}/");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Level_counts_ancestors(int depth)
    {
        var path = DepartmentPath.Build(null, Root);

        for (var i = 0; i < depth; i++)
        {
            path = DepartmentPath.Build(path, Guid.NewGuid());
        }

        DepartmentPath.LevelOf(path).ShouldBe(depth);
    }

    [Fact]
    public void A_department_is_inside_its_own_subtree()
    {
        var path = DepartmentPath.Build(null, Root);

        DepartmentPath.IsWithin(path, path).ShouldBeTrue();
    }

    [Fact]
    public void A_descendant_is_inside_its_ancestors_subtree()
    {
        var rootPath = DepartmentPath.Build(null, Root);
        var childPath = DepartmentPath.Build(rootPath, Child);
        var grandchildPath = DepartmentPath.Build(childPath, Grandchild);

        DepartmentPath.IsWithin(grandchildPath, rootPath).ShouldBeTrue();
        DepartmentPath.IsWithin(rootPath, grandchildPath).ShouldBeFalse();
    }

    [Fact]
    public void A_sibling_whose_id_shares_a_prefix_is_not_a_descendant()
    {
        // The reason the path is wrapped in separators. Without the trailing one,
        // "/a/b" would prefix-match "/a/bc" and a filter for one department would
        // silently pick up an unrelated sibling.
        var a = DepartmentPath.Build(null, new Guid("aaaaaaaa-0000-0000-0000-000000000001"));
        var b = DepartmentPath.Build(null, new Guid("aaaaaaaa-0000-0000-0000-000000000012"));

        DepartmentPath.IsWithin(b, a).ShouldBeFalse();
    }

    [Fact]
    public void Moving_a_department_beneath_its_own_descendant_is_a_cycle()
    {
        var rootPath = DepartmentPath.Build(null, Root);
        var childPath = DepartmentPath.Build(rootPath, Child);

        // Technology moved under Platform. Left unchecked the hierarchy becomes a ring:
        // the org chart recurses forever and the branch vanishes from every view that
        // walks down from a root.
        DepartmentPath.WouldCreateCycle(rootPath, childPath).ShouldBeTrue();
    }

    [Fact]
    public void Moving_a_department_beneath_an_unrelated_one_is_allowed()
    {
        var techPath = DepartmentPath.Build(null, Root);
        var salesPath = DepartmentPath.Build(null, Child);

        DepartmentPath.WouldCreateCycle(techPath, salesPath).ShouldBeFalse();
    }

    [Fact]
    public void Moving_a_department_to_the_top_level_is_allowed()
    {
        var path = DepartmentPath.Build(DepartmentPath.Build(null, Root), Child);

        DepartmentPath.WouldCreateCycle(path, null).ShouldBeFalse();
    }

    [Fact]
    public void Reparenting_rewrites_only_the_moved_prefix()
    {
        var oldParent = DepartmentPath.Build(null, Root);
        var newParent = DepartmentPath.Build(null, Child);
        var descendant = DepartmentPath.Build(oldParent, Grandchild);

        var moved = DepartmentPath.Reparent(descendant, oldParent, newParent);

        // The subtree keeps its shape below the move; only the ancestry above changes.
        moved.ShouldBe($"/{Child:D}/{Grandchild:D}/");
        DepartmentPath.LevelOf(moved).ShouldBe(1);
    }

    [Fact]
    public void Reparenting_leaves_a_path_outside_the_moved_subtree_untouched()
    {
        var unrelated = DepartmentPath.Build(null, Grandchild);

        DepartmentPath.Reparent(unrelated, DepartmentPath.Build(null, Root), DepartmentPath.Build(null, Child))
            .ShouldBe(unrelated);
    }

    [Fact]
    public void A_deep_subtree_survives_a_move_intact()
    {
        var oldRoot = DepartmentPath.Build(null, Root);
        var level1 = DepartmentPath.Build(oldRoot, Child);
        var level2 = DepartmentPath.Build(level1, Grandchild);

        var newRoot = DepartmentPath.Build(null, new Guid("44444444-4444-4444-4444-444444444444"));
        var movedRoot = DepartmentPath.Build(newRoot, Root);

        var movedLevel2 = DepartmentPath.Reparent(level2, oldRoot, movedRoot);

        DepartmentPath.LevelOf(movedLevel2).ShouldBe(3);
        DepartmentPath.IsWithin(movedLevel2, newRoot).ShouldBeTrue();
        movedLevel2.ShouldEndWith($"{Grandchild:D}/");
    }
}
