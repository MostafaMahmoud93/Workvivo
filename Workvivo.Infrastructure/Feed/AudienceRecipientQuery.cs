using Microsoft.EntityFrameworkCore;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Infrastructure.DBContext;

namespace Workvivo.Infrastructure.Feed;

/// <summary>
/// Turns a post's audience rows into the employees they select.
///
/// Every dimension except department is a direct column comparison. Department is the
/// awkward one: targeting a division must reach everyone beneath it, and the
/// hierarchy is arbitrarily deep. That is resolved in two steps - expand each targeted
/// department into its subtree using the materialised path, then match employees
/// against the expanded set - rather than with a recursive CTE, because the subtree
/// expansion is a prefix range scan on an indexed column and the recursive form is not.
/// </summary>
public sealed class AudienceRecipientQuery : IAudienceRecipientQuery
{
    private readonly Workvivo_DbContext _context;

    public AudienceRecipientQuery(Workvivo_DbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> GetPostRecipientsAsync(
        Guid postId,
        Guid? afterEmployeeId,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var audiences = await _context.Feed_PostAudiences
            .AsNoTracking()
            .Where(audience => audience.Post_Id == postId && !audience.Is_Deleted)
            .Select(audience => new { audience.Audience_Type, audience.Target_Id })
            .ToListAsync(cancellationToken);

        if (audiences.Count == 0)
        {
            return [];
        }

        var employees = _context.Org_Employees
            .AsNoTracking()
            .Where(employee => employee.Is_Active && !employee.Is_Deleted);

        if (afterEmployeeId is { } after)
        {
            employees = employees.Where(employee => employee.Id.CompareTo(after) > 0);
        }

        // Addressed to everybody: no further filtering, and no point building the rest.
        if (audiences.Exists(audience => audience.Audience_Type == AudienceType.AllEmployees))
        {
            return await Page(employees, batchSize, cancellationToken);
        }

        var targets = Targets.From(audiences.Select(a => (a.Audience_Type, a.Target_Id)));

        var departmentIds = await ExpandDepartmentsAsync(targets.Departments, cancellationToken);

        var filtered = employees.Where(employee =>
            targets.Employees.Contains(employee.Id)
            || (employee.Department_Id != null && departmentIds.Contains(employee.Department_Id.Value))
            || (employee.Team_Id != null && targets.Teams.Contains(employee.Team_Id.Value))
            || (employee.Location_Id != null && targets.Locations.Contains(employee.Location_Id.Value))
            || (employee.Job_Title_Id != null && targets.JobTitles.Contains(employee.Job_Title_Id.Value))
            || _context.UserGroupLinks.Any(link =>
                link.UserId == employee.User_Id && targets.Roles.Contains(link.RoleId))
            || _context.Comm_Members.Any(member =>
                member.Employee_Id == employee.Id
                && targets.Communities.Contains(member.Community_Id)
                && member.Membership_Status == MembershipStatus.Approved
                && !member.Is_Deleted));

        return await Page(filtered, batchSize, cancellationToken);
    }

    private static Task<List<Guid>> Page(
        IQueryable<Domain.Entities.Organization.Employee> employees,
        int batchSize,
        CancellationToken cancellationToken) =>
        employees
            .OrderBy(employee => employee.Id)
            .Take(batchSize)
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Every department at or below the targeted ones.
    ///
    /// One query per targeted department rather than one query with an OR over all of
    /// them. A post carries a handful of audience rows, so this is a handful of prefix
    /// scans - and each is a clean seek on the path index, which an OR of LIKE patterns
    /// is not.
    /// </summary>
    private async Task<HashSet<Guid>> ExpandDepartmentsAsync(
        IReadOnlyCollection<Guid> departmentIds,
        CancellationToken cancellationToken)
    {
        var expanded = new HashSet<Guid>();

        if (departmentIds.Count == 0)
        {
            return expanded;
        }

        var paths = await _context.Org_Departments
            .AsNoTracking()
            .Where(department => departmentIds.Contains(department.Id))
            .Select(department => department.Path)
            .ToListAsync(cancellationToken);

        foreach (var path in paths)
        {
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            var subtree = await _context.Org_Departments
                .AsNoTracking()
                .Where(department => department.Path.StartsWith(path))
                .Select(department => department.Id)
                .ToListAsync(cancellationToken);

            expanded.UnionWith(subtree);
        }

        return expanded;
    }

    /// <summary>The audience rows, split by dimension, as sets the query can test against.</summary>
    private sealed class Targets
    {
        public HashSet<Guid> Departments { get; } = [];
        public HashSet<Guid> Teams { get; } = [];
        public HashSet<Guid> Locations { get; } = [];
        public HashSet<Guid> JobTitles { get; } = [];
        public HashSet<Guid> Roles { get; } = [];
        public HashSet<Guid> Communities { get; } = [];
        public HashSet<Guid> Employees { get; } = [];

        public static Targets From(IEnumerable<(AudienceType Type, Guid? TargetId)> audiences)
        {
            var targets = new Targets();

            foreach (var (type, targetId) in audiences)
            {
                if (targetId is not { } id)
                {
                    continue;
                }

                switch (type)
                {
                    case AudienceType.Department: targets.Departments.Add(id); break;
                    case AudienceType.Team: targets.Teams.Add(id); break;
                    case AudienceType.Location: targets.Locations.Add(id); break;
                    case AudienceType.JobTitle: targets.JobTitles.Add(id); break;
                    case AudienceType.Role: targets.Roles.Add(id); break;
                    case AudienceType.Community: targets.Communities.Add(id); break;
                    case AudienceType.Employee: targets.Employees.Add(id); break;
                }
            }

            return targets;
        }
    }
}
