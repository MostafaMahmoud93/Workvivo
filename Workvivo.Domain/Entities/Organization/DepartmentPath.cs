namespace Workvivo.Domain.Entities.Organization;

/// <summary>
/// Builds and inspects the materialised ancestor path on a department.
///
/// The path is what makes "everything under this division" a single indexed range scan
/// instead of a recursive query - and that question is asked by audience targeting, the
/// org chart, and every departmental analytics filter.
///
/// Format is <c>/{ancestorId}/{...}/{ownId}/</c> using ids rather than names, so
/// renaming a department does not rewrite its subtree. The leading and trailing slashes
/// matter: without them <c>/a/b</c> would prefix-match <c>/a/bc</c>, and a filter for
/// one department would silently pick up an unrelated sibling.
/// </summary>
public static class DepartmentPath
{
    public const char Separator = '/';

    /// <summary>Path for a department directly under <paramref name="parentPath"/>.</summary>
    public static string Build(string? parentPath, Guid departmentId) =>
        string.IsNullOrEmpty(parentPath)
            ? $"{Separator}{departmentId:D}{Separator}"
            : $"{parentPath}{departmentId:D}{Separator}";

    /// <summary>Depth in the hierarchy; 0 for a top-level department.</summary>
    public static int LevelOf(string path) =>
        string.IsNullOrEmpty(path) ? 0 : path.Count(c => c == Separator) - 2;

    /// <summary>
    /// Prefix for a <c>LIKE</c> that matches a department and everything beneath it.
    /// </summary>
    public static string SubtreePrefix(string path) => path;

    /// <summary>
    /// Whether <paramref name="candidatePath"/> lies within the subtree rooted at
    /// <paramref name="ancestorPath"/>. A department is inside its own subtree.
    /// </summary>
    public static bool IsWithin(string candidatePath, string ancestorPath) =>
        !string.IsNullOrEmpty(ancestorPath)
        && candidatePath.StartsWith(ancestorPath, StringComparison.Ordinal);

    /// <summary>
    /// Whether moving a department under <paramref name="newParentPath"/> would make it
    /// its own ancestor.
    ///
    /// The check that stops a hierarchy becoming a ring. A cycle here is not a tidy
    /// failure: the org chart recurses forever and the subtree query returns nonsense,
    /// with no obvious culprit.
    /// </summary>
    public static bool WouldCreateCycle(string departmentPath, string? newParentPath) =>
        !string.IsNullOrEmpty(newParentPath) && IsWithin(newParentPath, departmentPath);

    /// <summary>
    /// Rewrites a descendant's path after its ancestor moved.
    ///
    /// Only the moved department's own prefix is replaced; everything below it keeps its
    /// relative shape, so a whole subtree is repointed by a single string operation per
    /// row rather than by rebuilding each path from the root.
    /// </summary>
    public static string Reparent(string descendantPath, string oldAncestorPath, string newAncestorPath) =>
        descendantPath.StartsWith(oldAncestorPath, StringComparison.Ordinal)
            ? newAncestorPath + descendantPath[oldAncestorPath.Length..]
            : descendantPath;
}
