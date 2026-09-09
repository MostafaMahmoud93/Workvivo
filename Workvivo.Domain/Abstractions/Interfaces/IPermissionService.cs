namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Resolves what a user is allowed to do.
///
/// The single place the permission model is interpreted. Everything else - the
/// endpoint attribute, the pipeline behaviour, the menu builder, the client's
/// capability list - asks this, so the resolution rules exist once.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Every named permission the user currently holds. Empty for an unknown,
    /// deactivated or deleted account.
    /// </summary>
    Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops one user's cached permissions. Call after any change to their roles or
    /// per-user overrides.
    /// </summary>
    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the cached permissions of everyone holding a role. Call after the role's
    /// own grants change - otherwise the change reaches people only as their individual
    /// entries expire.
    /// </summary>
    Task InvalidateRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
