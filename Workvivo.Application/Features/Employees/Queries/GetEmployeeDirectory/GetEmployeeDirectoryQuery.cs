using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Employees.Dtos;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Application.Features.Employees.Queries.GetEmployeeDirectory;

/// <summary>The staff directory: paged, filterable, searchable.</summary>
public sealed class GetEmployeeDirectoryQuery : PageRequest, IQuery<PagedResult<EmployeeListItemDto>>
{
    public Guid? DepartmentId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? JobTitleId { get; set; }

    /// <summary>
    /// Include the whole subtree beneath <see cref="DepartmentId"/> rather than only
    /// its direct members. "Everyone in Engineering" usually means the division.
    /// </summary>
    public bool IncludeSubDepartments { get; set; } = true;

    public bool OnlyFollowed { get; set; }
}

public sealed class GetEmployeeDirectoryQueryHandler
    : IRequestHandler<GetEmployeeDirectoryQuery, PagedResult<EmployeeListItemDto>>
{
    /// <summary>
    /// Columns the directory may be sorted by.
    ///
    /// An allow-list, because the sort field comes from the query string: resolving it
    /// by reflection or string-building the ORDER BY would let a caller name any column,
    /// including ones the projection deliberately omits.
    /// </summary>
    private static readonly Dictionary<string, System.Linq.Expressions.Expression<Func<Employee, object>>> Sortable =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = employee => employee.Display_Name,
            ["employeeNumber"] = employee => employee.Employee_Number,
            ["joiningDate"] = employee => employee.Joining_Date!,
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public GetEmployeeDirectoryQueryHandler(IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<EmployeeListItemDto>> Handle(
        GetEmployeeDirectoryQuery request,
        CancellationToken cancellationToken)
    {
        var viewerEmployeeId = await ResolveViewerAsync(cancellationToken);

        var query = _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Is_Active);

        if (request.DepartmentId is { } departmentId)
        {
            query = request.IncludeSubDepartments
                // Resolved through the materialised path: one indexed range scan
                // instead of walking the hierarchy.
                ? query.Where(employee =>
                    employee.Department != null
                    && _unitOfWork.Repository<Department, Guid>().GetAllQ()
                        .Where(department => department.Id == departmentId)
                        .Any(department => employee.Department.Path.StartsWith(department.Path)))
                : query.Where(employee => employee.Department_Id == departmentId);
        }

        if (request.TeamId is { } teamId)
        {
            query = query.Where(employee => employee.Team_Id == teamId);
        }

        if (request.LocationId is { } locationId)
        {
            query = query.Where(employee => employee.Location_Id == locationId);
        }

        if (request.JobTitleId is { } jobTitleId)
        {
            query = query.Where(employee => employee.Job_Title_Id == jobTitleId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // Parameterised by EF. A leading wildcard cannot use an index, which is
            // accepted here for a directory of this size - full-text search takes over
            // in the search phase.
            var term = request.Search.Trim();

            query = query.Where(employee =>
                EF.Functions.Like(employee.Display_Name, $"%{term}%")
                || EF.Functions.Like(employee.Employee_Number, $"%{term}%")
                || (employee.Full_Name_Ar != null && EF.Functions.Like(employee.Full_Name_Ar, $"%{term}%"))
                || EF.Functions.Like(employee.Email, $"%{term}%"));
        }

        if (request.OnlyFollowed && viewerEmployeeId is { } follower)
        {
            query = query.Where(employee =>
                _unitOfWork.Repository<EmployeeFollower, Guid>().GetAllQ()
                    .Any(follow => follow.Follower_Id == follower && follow.Followee_Id == employee.Id));
        }

        var sorted = query.ApplySort(request, Sortable, employee => employee.Display_Name);

        // The follow flag is a correlated EXISTS inside the same statement rather than
        // a second query per row - the classic N+1 this design is built to avoid.
        var projected = sorted.Select(employee => new EmployeeListItemDto
        {
            Id = employee.Id,
            DisplayName = employee.Display_Name,
            FullNameAr = employee.Full_Name_Ar,
            EmployeeNumber = employee.Employee_Number,
            JobTitle = employee.JobTitle == null ? null : employee.JobTitle.Name_En ?? employee.JobTitle.Name_Ar,
            Department = employee.Department == null ? null : employee.Department.Name_En ?? employee.Department.Name_Ar,
            Location = employee.Location == null ? null : employee.Location.Name_En ?? employee.Location.Name_Ar,
            Email = employee.Email,
            Mobile = employee.Mobile,
            ProfilePictureFileId = employee.Profile_Picture_File_Id,
            IsFollowedByMe = viewerEmployeeId != null
                && _unitOfWork.Repository<EmployeeFollower, Guid>().GetAllQ()
                    .Any(follow => follow.Follower_Id == viewerEmployeeId && follow.Followee_Id == employee.Id),
        });

        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    private async Task<Guid?> ResolveViewerAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return null;
        }

        return await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.User_Id == userId)
            .Select(employee => (Guid?)employee.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
