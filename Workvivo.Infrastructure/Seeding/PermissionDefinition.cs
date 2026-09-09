namespace Workvivo.Infrastructure.Seeding;

/// <summary>
/// One row of the permission catalogue.
///
/// A named record rather than a positional tuple so the seeder reads as data and the
/// tests that check it against the <see cref="Permissions"/> constants can do so by
/// property name instead of by reflection over compiler-generated tuple fields.
/// </summary>
/// <param name="Key">The named permission, for example <c>Post.Create</c>.</param>
/// <param name="Screen">Key of the screen this permission hangs off.</param>
/// <param name="Action">Id of the screen action it represents.</param>
/// <param name="Module">Grouping shown on the permission-management screen.</param>
internal sealed record PermissionDefinition(
    string Key,
    string Screen,
    Guid Action,
    string Module,
    string DescAr,
    string DescEn);
