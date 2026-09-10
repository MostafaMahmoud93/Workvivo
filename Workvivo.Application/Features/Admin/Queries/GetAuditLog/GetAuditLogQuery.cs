using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Admin.Dtos;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Audit;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Admin.Queries.GetAuditLog;

/// <summary>
/// The audit trail.
///
/// Read-only, and there is no endpoint that writes or deletes one. An audit log an
/// administrator can edit is not an audit log, and the value of the whole table
/// depends on that being true without exception.
/// </summary>
public sealed class GetAuditLogQuery : PageRequest, IQuery<PagedResult<AuditEntryDto>>
{
    public string? EntityName { get; set; }

    public Guid? UserId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}

public sealed class GetAuditLogQueryHandler
    : IRequestHandler<GetAuditLogQuery, PagedResult<AuditEntryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IPermissionService _permissions;

    public GetAuditLogQueryHandler(
        IUnitOfWork unitOfWork,
        CurrentEmployee currentEmployee,
        IPermissionService permissions)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
        _permissions = permissions;
    }

    public async Task<PagedResult<AuditEntryDto>> Handle(
        GetAuditLogQuery request,
        CancellationToken cancellationToken)
    {
        if (!await _permissions.HasPermissionAsync(
                _currentEmployee.UserId, PermissionKeys.AuditLogView, cancellationToken))
        {
            throw new ForbiddenException("You cannot read the audit log.", PermissionKeys.AuditLogView);
        }

        var query = _unitOfWork.Repository<AuditLog, long>().GetAllQ();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            var entity = request.EntityName.Trim();
            query = query.Where(entry => entry.Entity_Name == entity);
        }

        if (request.UserId is { } userId)
        {
            query = query.Where(entry => entry.User_Id == userId);
        }

        if (request.From is { } from)
        {
            query = query.Where(entry => entry.Timestamp >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(entry => entry.Timestamp < to);
        }

        return await query
            .OrderByDescending(entry => entry.Timestamp)
            .Select(entry => new AuditEntryDto
            {
                Id = entry.Id,
                Timestamp = entry.Timestamp,
                Username = entry.Username,
                Action = entry.Action.ToString(),
                EntityName = entry.Entity_Name,
                EntityId = entry.Entity_Id,
                AffectedColumns = entry.Affected_Columns,

                // The old and new values are deliberately not projected. They are
                // JSON of arbitrary content - salaries, personal details, draft
                // announcements - and a paged list is not where that should be
                // rendered by default.
                IpAddress = entry.Ip_Address,
                CorrelationId = entry.Correlation_Id,
            })
            .ToPagedResultAsync(request, cancellationToken);
    }
}
