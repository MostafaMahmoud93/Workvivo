namespace Workvivo.API.Extensions;

/// <summary>
/// Retired alongside <see cref="Workvivo.API.Filters.ActionFilter"/>.
///
/// This was a hard-coded list of controller and action names exempt from the
/// permission check. Every new public endpoint meant editing it, and forgetting to was
/// silent - the endpoint simply refused everyone. Worse, the two lists matched on bare
/// names, so an action called <c>GetCurrentUser</c> on any controller was exempt
/// everywhere.
///
/// Endpoints now say what they need themselves: <c>[AllowAnonymous]</c> for open ones,
/// <c>[HasPermission(...)]</c> for guarded ones, and the base controller's
/// <c>[Authorize]</c> for everything in between.
/// </summary>
[Obsolete("Endpoints declare their own [AllowAnonymous] or [HasPermission] attributes.")]
public static class AllowFiltered
{
    public static List<string> Controllers { get; } = [];

    public static List<string> Actions { get; } = [];
}
