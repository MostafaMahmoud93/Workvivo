using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Employees.Dtos;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Employees.Queries.GetEmployeeProfile;

/// <summary>One employee's profile. Pass no id for your own.</summary>
public sealed record GetEmployeeProfileQuery(Guid? EmployeeId = null) : IQuery<EmployeeProfileDto>;

public sealed class GetEmployeeProfileQueryHandler
    : IRequestHandler<GetEmployeeProfileQuery, EmployeeProfileDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetEmployeeProfileQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<EmployeeProfileDto> Handle(
        GetEmployeeProfileQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException();
        var employees = _unitOfWork.Repository<Employee, Guid>().GetAllQ();

        var viewerId = await employees
            .Where(employee => employee.User_Id == userId)
            .Select(employee => (Guid?)employee.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var targetId = request.EmployeeId ?? viewerId
            ?? throw new NotFoundException("You do not have an employee profile.");

        var managers = _unitOfWork.Repository<EmployeeManager, Guid>().GetAllQ();

        var profile = await employees
            .Where(employee => employee.Id == targetId)
            .Select(employee => new EmployeeProfileDto
            {
                Id = employee.Id,
                DisplayName = employee.Display_Name,
                FullNameAr = employee.Full_Name_Ar,
                FirstName = employee.First_Name,
                MiddleName = employee.Middle_Name,
                LastName = employee.Last_Name,
                EmployeeNumber = employee.Employee_Number,
                Email = employee.Email,
                Mobile = employee.Mobile,
                Extension = employee.Extension,
                Biography = employee.Biography_En ?? employee.Biography_Ar,

                DepartmentId = employee.Department_Id,
                DepartmentName = employee.Department == null ? null : employee.Department.Name_En ?? employee.Department.Name_Ar,
                TeamId = employee.Team_Id,
                TeamName = employee.Team == null ? null : employee.Team.Name_En ?? employee.Team.Name_Ar,
                LocationId = employee.Location_Id,
                LocationName = employee.Location == null ? null : employee.Location.Name_En ?? employee.Location.Name_Ar,
                JobTitleId = employee.Job_Title_Id,
                JobTitleName = employee.JobTitle == null ? null : employee.JobTitle.Name_En ?? employee.JobTitle.Name_Ar,

                ProfilePictureFileId = employee.Profile_Picture_File_Id,
                CoverPictureFileId = employee.Cover_Picture_File_Id,
                JoiningDate = employee.Joining_Date,

                // Day and month only, and only with consent. The year is the part that
                // identifies a person, and the product never needs it.
                BirthDay = employee.Show_Birthday && employee.Birth_Date != null
                    ? employee.Birth_Date!.Value.Day
                    : null,
                BirthMonth = employee.Show_Birthday && employee.Birth_Date != null
                    ? employee.Birth_Date!.Value.Month
                    : null,

                PreferredLanguage = employee.Preferred_Language,
                FollowersCount = employee.Followers_Count,
                FollowingCount = employee.Following_Count,
                RecognitionPoints = employee.Recognition_Points,
                IsActive = employee.Is_Active,

                IsMe = employee.Id == viewerId,
                IsFollowedByMe = viewerId != null
                    && _unitOfWork.Repository<EmployeeFollower, Guid>().GetAllQ()
                        .Any(follow => follow.Follower_Id == viewerId && follow.Followee_Id == employee.Id),

                Manager = managers
                    .Where(link => link.Employee_Id == employee.Id
                        && link.Is_Primary
                        && link.Effective_To == null
                        && link.Manager != null)
                    .Select(link => new ManagerSummaryDto
                    {
                        Id = link.Manager!.Id,
                        DisplayName = link.Manager.Display_Name,
                        JobTitle = link.Manager.JobTitle == null ? null : link.Manager.JobTitle.Name_En,
                        ProfilePictureFileId = link.Manager.Profile_Picture_File_Id,
                    })
                    .FirstOrDefault(),

                DirectReports = managers
                    .Where(link => link.Manager_Id == employee.Id
                        && link.Effective_To == null
                        && link.Employee != null
                        && link.Employee.Is_Active)
                    .Select(link => new ManagerSummaryDto
                    {
                        Id = link.Employee!.Id,
                        DisplayName = link.Employee.Display_Name,
                        JobTitle = link.Employee.JobTitle == null ? null : link.Employee.JobTitle.Name_En,
                        ProfilePictureFileId = link.Employee.Profile_Picture_File_Id,
                    })
                    .ToList(),

                Skills = _unitOfWork.Repository<EmployeeSkill, Guid>().GetAllQ()
                    .Where(link => link.Employee_Id == employee.Id && link.Skill != null)
                    .OrderByDescending(link => link.Endorsement_Count)
                    .Select(link => new SkillDto
                    {
                        Id = link.Skill!.Id,
                        Name = link.Skill.Name_En ?? link.Skill.Name_Ar,
                        EndorsementCount = link.Endorsement_Count,
                    })
                    .ToList(),

                Interests = _unitOfWork.Repository<EmployeeInterest, Guid>().GetAllQ()
                    .Where(link => link.Employee_Id == employee.Id && link.Interest != null)
                    .Select(link => link.Interest!.Name_En ?? link.Interest.Name_Ar)
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return profile ?? throw new NotFoundException(nameof(Employee), targetId);
    }
}
