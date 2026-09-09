using System.Reflection;
using Shouldly;
using Workvivo.Infrastructure.Abstractions;
using Workvivo.Infrastructure.Seeding;
using Xunit;

namespace Workvivo.Application.Tests.Seeding;

/// <summary>
/// Guards the seam between the <see cref="Permissions"/> constants and the seeded
/// permission catalogue.
///
/// The failure this prevents is silent. Adding a constant and forgetting the seed row
/// produces a permission that exists in code, compiles, is accepted by
/// <c>[HasPermission]</c> - and that nobody holds, so the endpoint refuses everyone
/// including administrators. Nothing throws; the feature simply does not work, and the
/// cause is several layers away from the symptom.
/// </summary>
public class PermissionCatalogueTests
{
    /// <summary>Every constant declared on <see cref="Permissions"/> and its nested classes.</summary>
    private static IReadOnlyList<string> DeclaredPermissions() =>
    [
        .. typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(group => group.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!),
    ];

    /// <summary>The catalogue exactly as the migration was generated from.</summary>
    private static IReadOnlyList<string> SeededPermissionKeys() =>
        [.. ReferenceDataSeeder.PermissionDefinitions.Select(definition => definition.Key)];

    [Fact]
    public void Every_declared_permission_is_seeded()
    {
        var missing = DeclaredPermissions().Except(SeededPermissionKeys()).ToArray();

        missing.ShouldBeEmpty(
            "these permissions exist as constants but have no catalogue row, so no role can "
            + "ever hold them: " + string.Join(", ", missing));
    }

    [Fact]
    public void Every_seeded_permission_has_a_declared_constant()
    {
        // The other direction: a seeded row nobody references is dead weight on the
        // permission-management screen, and usually means a constant was renamed.
        var orphaned = SeededPermissionKeys().Except(DeclaredPermissions()).ToArray();

        orphaned.ShouldBeEmpty(
            "these catalogue rows have no matching constant: " + string.Join(", ", orphaned));
    }

    [Fact]
    public void Permission_keys_are_unique()
    {
        var keys = SeededPermissionKeys();

        // A duplicate would violate the filtered unique index at migration time, but
        // failing here names the offender instead of surfacing as a SQL error.
        keys.Distinct().Count().ShouldBe(keys.Count);
    }

    [Fact]
    public void Permission_keys_follow_the_Area_Action_convention()
    {
        foreach (var key in SeededPermissionKeys())
        {
            var parts = key.Split('.');

            parts.Length.ShouldBe(2, $"'{key}' should be in the form Area.Action");
            parts.ShouldAllBe(part => part.Length > 0);
        }
    }

    [Fact]
    public void The_employee_role_holds_only_read_and_post_permissions()
    {
        // Least privilege, pinned. Widening the default role is a decision someone
        // should have to make deliberately - and this test makes them.
        ReferenceDataSeeder.RolePermissions[Roles.Employee].ShouldBe(
            [
                Permissions.Employee.View,
                Permissions.Post.View,
                Permissions.Post.Create,
                Permissions.Document.View,
            ],
            ignoreOrder: true);
    }

    [Fact]
    public void Super_admin_holds_every_permission()
    {
        // Granted explicitly rather than by bypassing the check, so the permission
        // screen shows the truth and every grant is auditable.
        ReferenceDataSeeder.RolePermissions[Roles.SuperAdmin].ShouldBe(SeededPermissionKeys(), ignoreOrder: true);
    }

    [Fact]
    public void Deterministic_guids_are_stable_and_distinct()
    {
        // The whole seed depends on this. If it ever stopped being stable, every
        // migration would report pending model changes and refuse to run - which is
        // exactly the failure this project already hit once.
        DeterministicGuid.From("perm:Post.Create")
            .ShouldBe(DeterministicGuid.From("perm:Post.Create"));

        DeterministicGuid.From("perm:Post.Create")
            .ShouldNotBe(DeterministicGuid.From("perm:Post.Edit"));

        DeterministicGuid.From("perm:Post.Create").ShouldNotBe(Guid.Empty);
    }
}
