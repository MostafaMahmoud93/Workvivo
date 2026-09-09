using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Organization.Dtos;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Application.Features.Organization.Queries.GetDepartmentTree;

/// <summary>The whole department hierarchy, as a tree.</summary>
public sealed record GetDepartmentTreeQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<DepartmentNodeDto>>;

public sealed class GetDepartmentTreeQueryHandler
    : IRequestHandler<GetDepartmentTreeQuery, IReadOnlyList<DepartmentNodeDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDepartmentTreeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<DepartmentNodeDto>> Handle(
        GetDepartmentTreeQuery request,
        CancellationToken cancellationToken)
    {
        // One flat query, assembled into a tree in memory.
        //
        // An organisation has hundreds of departments, not millions, so fetching them
        // all once beats a recursive CTE or - far worse - a query per level. The
        // employee counts come from a grouped subquery in the same statement rather
        // than a count per node, which would be the N+1 this design exists to avoid.
        var employeeCounts = _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Department_Id != null && employee.Is_Active)
            .GroupBy(employee => employee.Department_Id!.Value)
            .Select(group => new { DepartmentId = group.Key, Count = group.Count() });

        var flat = await _unitOfWork.Repository<Department, Guid>()
            .GetAllQ()
            .Where(department => request.IncludeInactive || department.Is_Active)
            .OrderBy(department => department.Level)
            .ThenBy(department => department.Sort_Order)
            .ThenBy(department => department.Name_Ar)
            .Select(department => new DepartmentNodeDto
            {
                Id = department.Id,
                Name = department.Name_En ?? department.Name_Ar,
                Code = department.Code,
                ParentDepartmentId = department.Parent_Department_Id,
                ManagerEmployeeId = department.Manager_Employee_Id,
                ManagerName = department.Manager == null ? null : department.Manager.Display_Name,
                Level = department.Level,
                EmployeeCount = employeeCounts
                    .Where(count => count.DepartmentId == department.Id)
                    .Select(count => count.Count)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return BuildTree(flat);
    }

    /// <summary>
    /// Links the flat list into a tree.
    ///
    /// Ordered by level, so a parent is always seen before its children and one pass is
    /// enough. A node whose parent is missing - filtered out as inactive, or orphaned by
    /// bad data - is promoted to a root rather than dropped, because silently losing a
    /// department from the org chart is worse than showing it in the wrong place.
    /// </summary>
    private static List<DepartmentNodeDto> BuildTree(List<DepartmentNodeDto> flat)
    {
        var byId = flat.ToDictionary(node => node.Id);
        var roots = new List<DepartmentNodeDto>();

        foreach (var node in flat)
        {
            if (node.ParentDepartmentId is { } parentId && byId.TryGetValue(parentId, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots;
    }
}
