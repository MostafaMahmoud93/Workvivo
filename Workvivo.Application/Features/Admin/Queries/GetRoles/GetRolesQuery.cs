using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Admin.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Admin.Queries.GetRoles;

/// <summary>The roles, with how many people hold each.</summary>
public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleDto>>;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    /// <summary>
    /// The roles the platform seeds and relies on.
    ///
    /// Named here rather than flagged in the database because the set is a property
    /// of the code that seeds them, and a column would drift the first time somebody
    /// edited a row by hand.
    /// </summary>
    private static readonly string[] SystemRoles =
    [
        Roles.SuperAdmin,
        Roles.Admin,
        Roles.Hr,
        Roles.CommunicationsManager,
        Roles.DepartmentManager,
        Roles.Moderator,
        Roles.Employee,
    ];

    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IPermissionService _permissions;

    public GetRolesQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IPermissionService permissions)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _permissions = permissions;
    }

    public async Task<IReadOnlyList<RoleDto>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        if (!await _permissions.HasPermissionAsync(
                _currentEmployee.UserId, PermissionKeys.RoleManage, cancellationToken))
        {
            throw new ForbiddenException("You cannot manage roles.", PermissionKeys.RoleManage);
        }

        // Counted through the role's own navigation rather than a second query
        // against the membership table. UserGroupsLink is an Identity join type, not
        // one of the platform's entities, so it has no generic repository - and
        // reaching past the abstraction into the DbContext for a count would be a
        // worse trade than one extra subquery over a handful of roles.
        var roles = await _unitOfWork.UserGroupManager.Roles
            .Select(role => new
            {
                role.Id,
                role.Name,
                role.Name_Ar,
                role.Name_En,
                MemberCount = role.UserRoles.Count(),
            })
            .ToListAsync(cancellationToken);

        var roleIds = roles.ConvertAll(role => role.Id);

        var grants = await _unitOfWork.GroupPermissionsRepository
            .GetAllQ()
            .Where(grant => roleIds.Contains(grant.Group_Id))
            .GroupBy(grant => grant.Group_Id)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return
        [
            .. roles.Select(role => new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,

                // The human-readable label, which is bilingual - Name is the Identity
                // key and is not shown to anybody.
                Description = LocalizedText.Pick(role.Name_Ar, role.Name_En),
                MemberCount = role.MemberCount,
                PermissionCount = grants.Find(row => row.RoleId == role.Id)?.Count ?? 0,
                IsSystem = Array.Exists(
                    SystemRoles,
                    name => string.Equals(name, role.Name, StringComparison.OrdinalIgnoreCase)),
            })
            .OrderBy(role => role.Name),
        ];
    }
}
