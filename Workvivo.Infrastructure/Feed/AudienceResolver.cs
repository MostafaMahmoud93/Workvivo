using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Infrastructure.Caching;
using Workvivo.Infrastructure.DBContext;

namespace Workvivo.Infrastructure.Feed;

/// <summary>
/// Builds an employee's audience key set and caches it.
///
/// Cached because it is needed on every feed request, every document list and every
/// event calendar, and it changes only when somebody transfers, changes role, or joins
/// a community. Ten minutes is a compromise: long enough that the hot path almost never
/// pays for it, short enough that a transfer becomes visible without anyone having to
/// remember to invalidate.
///
/// It is not an access-control boundary on its own. Document downloads and community
/// posts are checked again at the point of access; this decides what appears in a list.
/// </summary>
public sealed class AudienceResolver : IAudienceResolver
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    private readonly Workvivo_DbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<AudienceResolver> _logger;

    public AudienceResolver(
        Workvivo_DbContext context,
        ICacheService cache,
        ILogger<AudienceResolver> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ResolveKeysAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return [AudienceKey.All];
        }

        var cacheKey = CacheKeys.Audience.ForEmployee(employeeId);

        var cached = await _cache.GetAsync<string[]>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var keys = await BuildAsync(employeeId, cancellationToken);

        await _cache.SetAsync(cacheKey, keys, CacheLifetime, cancellationToken);

        return keys;
    }

    public async Task InvalidateAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.Audience.ForEmployee(employeeId), cancellationToken);
        _logger.LogDebug("Invalidated the audience key set for employee {EmployeeId}", employeeId);
    }

    private async Task<string[]> BuildAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var profile = await _context.Org_Employees
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => new
            {
                employee.Id,
                employee.User_Id,
                employee.Team_Id,
                employee.Location_Id,
                employee.Job_Title_Id,

                // The department's materialised path comes back with the employee, so
                // the ancestor keys below need no second query.
                DepartmentPath = employee.Department == null ? null : employee.Department.Path,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            // No employee record - a service account, or a user provisioned but not yet
            // onboarded. They see only what is addressed to everybody.
            return [AudienceKey.All];
        }

        var keys = new List<string>(24)
        {
            AudienceKey.All,
            AudienceKey.For(AudienceType.Employee, profile.Id),
        };

        // Every ancestor department, not just the employee's own.
        //
        // This is the part that is easy to get wrong. A post targeted at "Technology"
        // has to reach somebody in "Platform" three levels below it - otherwise a
        // division-wide announcement silently misses most of the division. The
        // materialised path already holds the ancestry, so each id in it becomes a key.
        foreach (var departmentId in ParseAncestorIds(profile.DepartmentPath))
        {
            keys.Add(AudienceKey.For(AudienceType.Department, departmentId));
        }

        if (profile.Team_Id is { } teamId)
        {
            keys.Add(AudienceKey.For(AudienceType.Team, teamId));
        }

        if (profile.Location_Id is { } locationId)
        {
            keys.Add(AudienceKey.For(AudienceType.Location, locationId));
        }

        if (profile.Job_Title_Id is { } jobTitleId)
        {
            keys.Add(AudienceKey.For(AudienceType.JobTitle, jobTitleId));
        }

        var roleIds = await _context.UserGroupLinks
            .AsNoTracking()
            .Where(link => link.UserId == profile.User_Id)
            .Select(link => link.RoleId)
            .ToListAsync(cancellationToken);

        keys.AddRange(roleIds.Select(roleId => AudienceKey.For(AudienceType.Role, roleId)));

        // Approved memberships only. A pending request must not grant sight of the
        // community's content - that is precisely what the approval is for.
        var communityIds = await _context.Comm_Members
            .AsNoTracking()
            .Where(member => member.Employee_Id == employeeId
                && member.Membership_Status == MembershipStatus.Approved
                && !member.Is_Deleted)
            .Select(member => member.Community_Id)
            .ToListAsync(cancellationToken);

        keys.AddRange(communityIds.Select(communityId => AudienceKey.For(AudienceType.Community, communityId)));

        return [.. keys.Distinct(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Pulls the department ids out of a materialised path.
    ///
    /// The path is <c>/{ancestor}/{...}/{own}/</c>, so splitting it yields the whole
    /// chain from the root down to the employee's own department - exactly the set of
    /// departments whose targeted content should reach them.
    /// </summary>
    private static IEnumerable<Guid> ParseAncestorIds(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            yield break;
        }

        foreach (var segment in path.Split(DepartmentPath.Separator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(segment, out var id))
            {
                yield return id;
            }
        }
    }
}
