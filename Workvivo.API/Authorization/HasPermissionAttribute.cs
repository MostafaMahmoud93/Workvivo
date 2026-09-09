using Microsoft.AspNetCore.Authorization;

namespace Workvivo.API.Authorization;

/// <summary>
/// Requires a named permission on an endpoint:
/// <c>[HasPermission(Permissions.Post.Create)]</c>.
///
/// Built on the framework's own policy mechanism rather than on a custom action filter.
/// That matters for a reason beyond tidiness: the template's previous filter called
/// <c>.Result</c> on an async permission lookup, which blocks a request thread and, on
/// a busy server, deadlocks. An authorisation handler is awaited properly.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>Prefix that marks a policy name as a permission requirement.</summary>
    public const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permission)
        : base(PolicyPrefix + permission)
    {
        Permission = permission;
    }

    public string Permission { get; }

    /// <summary>Extracts the permission from a policy name, or null if it is not one.</summary>
    public static string? ExtractPermission(string policyName) =>
        policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase)
            ? policyName[PolicyPrefix.Length..]
            : null;
}
