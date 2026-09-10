using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Admin.Dtos;
using Workvivo.Application.Features.Admin.Queries.GetAuditLog;
using Workvivo.Application.Features.Admin.Queries.GetRoles;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.API.Controllers.Platform;

/// <summary>
/// Administration.
///
/// Every endpoint carries its own permission rather than the controller carrying one
/// for all of them. Reading the audit log and changing roles are different powers,
/// and an auditor who can read the log should not thereby be able to grant
/// themselves anything.
/// </summary>
public class AdminController : ApiControllersBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.Admin.Roles)]
    [HasPermission(Permissions.Role.Manage)]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(
            await _sender.Send(new GetRolesQuery(), cancellationToken)));

    /// <summary>
    /// The audit trail. Read-only by design - there is no endpoint that writes or
    /// deletes an entry, because a log an administrator can edit is not a log.
    /// </summary>
    [HttpGet]
    [Route(RouteClass.Admin.AuditLog)]
    [HasPermission(Permissions.AuditLog.View)]
    public async Task<IActionResult> AuditLog(
        [FromQuery] GetAuditLogQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<AuditEntryDto>>.Ok(await _sender.Send(query, cancellationToken)));
}
