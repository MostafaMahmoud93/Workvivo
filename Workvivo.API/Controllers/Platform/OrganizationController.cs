using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Features.Organization.Commands.DeleteDepartment;
using Workvivo.Application.Features.Organization.Commands.SaveDepartment;
using Workvivo.Application.Features.Organization.Dtos;
using Workvivo.Application.Features.Organization.Queries.GetDepartmentTree;
using Workvivo.Application.Features.Organization.Queries.GetOrganizationLookups;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.API.Controllers.Platform;

public class OrganizationController : ApiControllersBase
{
    private readonly ISender _sender;

    public OrganizationController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// The org chart. Readable by anyone who can see the directory - knowing the shape
    /// of the company is the point of an intranet.
    /// </summary>
    [HttpGet]
    [Route(RouteClass.OrganizationRoutes.DepartmentTree)]
    [HasPermission(Permissions.Employee.View)]
    public async Task<IActionResult> DepartmentTree(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<DepartmentNodeDto>>.Ok(
            await _sender.Send(new GetDepartmentTreeQuery(includeInactive), cancellationToken)));

    [HttpGet]
    [Route(RouteClass.OrganizationRoutes.Lookups)]
    [HasPermission(Permissions.Employee.View)]
    public async Task<IActionResult> Lookups(CancellationToken cancellationToken) =>
        Ok(ApiResponse<OrganizationLookupsDto>.Ok(
            await _sender.Send(new GetOrganizationLookupsQuery(), cancellationToken)));

    [HttpPost]
    [Route(RouteClass.OrganizationRoutes.SaveDepartment)]
    [HasPermission(Permissions.Organization.Manage)]
    public async Task<IActionResult> SaveDepartment(
        SaveDepartmentCommand command,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<Guid>.Ok(await _sender.Send(command, cancellationToken)));

    [HttpDelete]
    [Route(RouteClass.OrganizationRoutes.DeleteDepartment)]
    [HasPermission(Permissions.Organization.Manage)]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteDepartmentCommand(id), cancellationToken);
        return Ok(ApiResponse.Ok("Department removed."));
    }
}
