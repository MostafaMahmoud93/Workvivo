using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Employees.Commands.FollowEmployee;
using Workvivo.Application.Features.Employees.Commands.UpdateMyProfile;
using Workvivo.Application.Features.Employees.Dtos;
using Workvivo.Application.Features.Employees.Queries.GetEmployeeDirectory;
using Workvivo.Application.Features.Employees.Queries.GetEmployeeProfile;
using Workvivo.Application.Features.Employees.Queries.SearchEmployees;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.API.Controllers.Platform;

public class EmployeesController : ApiControllersBase
{
    private readonly ISender _sender;

    public EmployeesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.Employees.Directory)]
    [HasPermission(Permissions.Employee.View)]
    public async Task<IActionResult> Directory(
        [FromQuery] GetEmployeeDirectoryQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<EmployeeListItemDto>>.Ok(await _sender.Send(query, cancellationToken)));

    /// <summary>
    /// Type-ahead for the mention picker.
    ///
    /// Employee.View rather than open to any signed-in user: it is a name-completion
    /// endpoint over the whole staff list, which is exactly the shape of thing worth
    /// enumerating.
    /// </summary>
    [HttpGet]
    [Route(RouteClass.Employees.Suggest)]
    [HasPermission(Permissions.Employee.View)]
    public async Task<IActionResult> Suggest(
        [FromQuery] string term,
        [FromQuery] int limit,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<EmployeeSuggestionDto>>.Ok(
            await _sender.Send(new SearchEmployeesQuery(term, limit == 0 ? 10 : limit), cancellationToken)));

    /// <summary>
    /// The caller's own profile. No permission needed and no id accepted - the record
    /// is resolved from the authenticated principal.
    /// </summary>
    [HttpGet]
    [Route(RouteClass.Employees.Me)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        Ok(ApiResponse<EmployeeProfileDto>.Ok(
            await _sender.Send(new GetEmployeeProfileQuery(), cancellationToken)));

    [HttpPut]
    [Route(RouteClass.Employees.UpdateMe)]
    public async Task<IActionResult> UpdateMe(
        UpdateMyProfileCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok("Profile updated."));
    }

    [HttpGet]
    [Route(RouteClass.Employees.Profile)]
    [HasPermission(Permissions.Employee.View)]
    public async Task<IActionResult> Profile(Guid employeeId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<EmployeeProfileDto>.Ok(
            await _sender.Send(new GetEmployeeProfileQuery(employeeId), cancellationToken)));

    /// <summary>
    /// Following is a personal action on the caller's own behalf, so it needs no
    /// management permission - only the directory permission that let them find the
    /// colleague in the first place.
    /// </summary>
    [HttpPost]
    [Route(RouteClass.Employees.Follow)]
    [HasPermission(Permissions.Employee.View)]
    public async Task<IActionResult> Follow(Guid employeeId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<bool>.Ok(
            await _sender.Send(new FollowEmployeeCommand(employeeId, Follow: true), cancellationToken)));

    [HttpDelete]
    [Route(RouteClass.Employees.Unfollow)]
    [HasPermission(Permissions.Employee.View)]
    public async Task<IActionResult> Unfollow(Guid employeeId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<bool>.Ok(
            await _sender.Send(new FollowEmployeeCommand(employeeId, Follow: false), cancellationToken)));
}
