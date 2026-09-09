namespace Workvivo.API.Filters;

/// <summary>
/// Retired. Replaced by <see cref="Workvivo.API.Authorization.PermissionAuthorizationHandler"/>.
///
/// The original blocked on an async permission lookup with <c>.Result</c>, which ties
/// up a request thread for the duration of a database round trip and deadlocks under
/// load. It also matched permissions by URL prefix, so the route and the permission
/// had to be kept in step by hand, and it consulted a hard-coded allow-list of
/// controller and action names to decide what to skip.
///
/// The replacement is declarative - <c>[HasPermission(Permissions.Post.Create)]</c> -
/// awaited properly, and driven by the same database rows that drive the menu.
///
/// The type is kept as an empty pass-through rather than deleted so that any
/// registration still referencing it keeps compiling. Remove it once the solution is
/// confirmed clean of references.
/// </summary>
[Obsolete("Use [HasPermission] and PermissionAuthorizationHandler instead.")]
public class ActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
