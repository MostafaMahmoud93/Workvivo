using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Workvivo.API.Authorization;

/// <summary>
/// Creates an authorisation policy on demand for any <c>Permission:*</c> name.
///
/// The alternative is registering a policy per permission at start-up, which means
/// every new permission needs a line in Program.cs and a deployment. Since permissions
/// are rows in a table that administrators can grant and revoke, the set is not known
/// at compile time and the policy has to be manufactured when it is first asked for.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        // Anything that is not a permission policy - a plain [Authorize], a named
        // policy registered the usual way - still goes through the framework's own
        // provider.
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var permission = HasPermissionAttribute.ExtractPermission(policyName);

        if (permission is null)
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
