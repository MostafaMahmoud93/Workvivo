using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Infrastructure.Caching;
using Workvivo.Infrastructure.DBContext;

namespace Workvivo.Infrastructure.Identity;

/// <summary>
/// Reads the permission model out of <c>VW_UserActions</c> and caches the result per
/// user.
///
/// The view already encodes the resolution rules - deny beats grant, roles contribute,
/// deactivated accounts resolve to nothing - so this type does not re-implement them.
/// Its job is to make the answer cheap: an authorisation check happens on every
/// request, and hitting a five-way join each time is not viable.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    /// <summary>
    /// How long a resolved permission set is trusted.
    ///
    /// This is the window in which a revoked permission can still be honoured, so it is
    /// deliberately short. Every path that changes permissions also invalidates
    /// explicitly; the expiry is the backstop for anything that does not, such as a
    /// direct database edit.
    /// </summary>
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private readonly Workvivo_DbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        Workvivo_DbContext context,
        ICacheService cache,
        ILogger<PermissionService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return FrozenEmpty;
        }

        var cached = await _cache.GetAsync<string[]>(
            CacheKeys.Permissions.ForUser(userId), cancellationToken);

        if (cached is not null)
        {
            return cached.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var permissions = await _context.VW_UserActions
            .AsNoTracking()
            .Where(row => row.User_Id == userId && row.Permission_Key != null)
            .Select(row => row.Permission_Key!)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        // An empty set is cached too, unlike the general rule in the cache service.
        // Here "no permissions" is a real, common answer - most staff hold few - and
        // not caching it would mean the cheapest users to authorise are the ones that
        // hit the database on every single request.
        await _cache.SetAsync(
            CacheKeys.Permissions.ForUser(userId), permissions, CacheLifetime, cancellationToken);

        return permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        var permissions = await GetPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permission);
    }

    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.Permissions.ForUser(userId), cancellationToken);
        _logger.LogInformation("Invalidated cached permissions for user {UserId}", userId);
    }

    public async Task InvalidateRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        // Changing a role's grants changes what every holder of that role can do. Their
        // cached sets are per-user, so each has to be dropped - otherwise the change
        // arrives gradually as individual entries expire, which is confusing to
        // diagnose and, for a revocation, unsafe.
        var affected = await _context.UserGroupLinks
            .AsNoTracking()
            .Where(link => link.RoleId == roleId)
            .Select(link => link.UserId)
            .ToArrayAsync(cancellationToken);

        foreach (var userId in affected)
        {
            await _cache.RemoveAsync(CacheKeys.Permissions.ForUser(userId), cancellationToken);
        }

        _logger.LogInformation(
            "Invalidated cached permissions for {Count} users after a change to role {RoleId}",
            affected.Length,
            roleId);
    }

    private static readonly HashSet<string> FrozenEmpty = new(StringComparer.OrdinalIgnoreCase);
}
