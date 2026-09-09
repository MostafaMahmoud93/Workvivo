using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.API.Authorization;

/// <summary>
/// Answers a <see cref="PermissionRequirement"/> by asking the permission service.
///
/// Permissions are resolved server-side rather than read from claims in the token.
/// A token is issued once and cannot be recalled, so baking permissions into it would
/// mean a revoked permission keeps working until the token expires. Resolving per
/// request bounds that to the permission cache's lifetime instead - and keeps the token
/// small, which matters when it is sent on every request.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionService permissionService,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            // Not a failure: leaving the requirement unmet lets the framework issue a
            // 401 challenge rather than a 403, which is the right answer for someone
            // who has not signed in.
            return;
        }

        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            _logger.LogWarning("Authenticated principal has no usable subject claim; denying {Permission}",
                requirement.Permission);
            return;
        }

        if (await _permissionService.HasPermissionAsync(userId, requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogInformation(
            "User {UserId} was denied {Permission}",
            userId,
            requirement.Permission);
    }
}
