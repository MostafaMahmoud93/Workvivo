using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Notifications.Dtos;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.RealTime;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Notifications.Queries.GetUnreadCount;

/// <summary>
/// The number on the bell.
///
/// Not cached. It is polled on every page load by every employee, so it looks like an
/// obvious cache - but it changes the moment anything arrives, and a stale badge is
/// worse than no badge. The filtered index on unread rows makes it a seek.
/// </summary>
public sealed record GetUnreadCountQuery : IQuery<UnreadCountDto>;

public sealed class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, UnreadCountDto>
{
    /// <summary>
    /// Ceiling on the count. Past this the client shows "99+", and counting further is
    /// work nobody reads.
    /// </summary>
    public const int MaxCounted = 99;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public GetUnreadCountQueryHandler(IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<UnreadCountDto> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException();

        var unread = await _unitOfWork.Repository<NotificationUser, Guid>()
            .GetAllQ()
            .Where(delivery => delivery.Reciever_Id == userId && !delivery.IS_Seen)
            .Take(MaxCounted + 1)
            .CountAsync(cancellationToken);

        return new UnreadCountDto { Unread = unread };
    }
}
