using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Notifications.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.RealTime;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Features.Notifications.Queries.GetMyNotifications;

/// <summary>
/// The signed-in person's notifications, newest first.
///
/// There is no recipient parameter, and there must never be one. The recipient is the
/// caller, taken from the token - an endpoint that accepted an employee id here would
/// let anybody read anybody's notifications, which is a straight information
/// disclosure and the easiest kind to ship by accident.
/// </summary>
public sealed class GetMyNotificationsQuery : CursorRequest, IQuery<CursorPagedResult<NotificationDto>>
{
    /// <summary>When true, only what has not been read yet.</summary>
    public bool UnreadOnly { get; set; }
}

public sealed class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, CursorPagedResult<NotificationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public GetMyNotificationsQueryHandler(IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CursorPagedResult<NotificationDto>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException();

        var query = _unitOfWork.Repository<NotificationUser, Guid>()
            .GetAllQ()
            .Where(delivery => delivery.Reciever_Id == userId);

        if (request.UnreadOnly)
        {
            query = query.Where(delivery => !delivery.IS_Seen);
        }

        if (Cursor.TryDecode(request.Cursor, out var cursorDate, out var cursorId))
        {
            // Tuple comparison, expanded by hand: EF cannot translate
            // ValueTuple.CompareTo, and comparing the date alone would drop rows that
            // share a millisecond with the last one on the previous page.
            query = query.Where(delivery =>
                delivery.Create_Date < cursorDate
                || (delivery.Create_Date == cursorDate && delivery.Id.CompareTo(cursorId) < 0));
        }

        var page = await query
            .OrderByDescending(delivery => delivery.Create_Date)
            .ThenByDescending(delivery => delivery.Id)
            .Select(Projection)
            .ToCursorPagedResultAsync(
                request.PageSize,
                row => (row.CreatedDate, row.Id),
                cancellationToken);

        return Localize(page);
    }

    /// <summary>
    /// Held as an expression, not written as a method.
    ///
    /// A static method here would be invoked in memory: EF gives up translating,
    /// materialises the entity, and the lazy-loading proxy then fires a query per row
    /// for the actor - while the page's reader is still open.
    /// </summary>
    private static readonly System.Linq.Expressions.Expression<Func<NotificationUser, Row>> Projection =
        delivery => new Row
        {
            Id = delivery.Id,
            NotificationId = delivery.Notification_Id,
            Type = delivery.Notification.Notification_Type,
            HeaderAr = delivery.Notification.Header_Ar,
            HeaderEn = delivery.Notification.Header_En,
            ContentAr = delivery.Notification.Content_Ar,
            ContentEn = delivery.Notification.Content_En,
            RedirectUrl = delivery.Notification.RedirectUrl,
            EntityType = (int)delivery.Notification.Entity_Type,
            EntityId = delivery.Notification.Entity_Id,
            ActorEmployeeId = delivery.Notification.Actor_Employee_Id,
            ActorDisplayName = delivery.Notification.Actor == null
                ? null
                : delivery.Notification.Actor.Display_Name,
            ActorProfilePictureFileId = delivery.Notification.Actor == null
                ? null
                : delivery.Notification.Actor.Profile_Picture_File_Id,
            IsSeen = delivery.IS_Seen,
            CreatedDate = delivery.Create_Date,
        };

    /// <summary>
    /// Picks the reader's language.
    ///
    /// Done here rather than in the projection because the choice depends on the
    /// request culture, which is a runtime value the database knows nothing about -
    /// and a CASE over the culture in SQL would be recompiled for every locale.
    /// </summary>
    private static CursorPagedResult<NotificationDto> Localize(CursorPagedResult<Row> page)
    {
        var items = page.Items
            .Select(row => new NotificationDto
            {
                Id = row.Id,
                NotificationId = row.NotificationId,
                Type = row.Type,
                Header = LocalizedText.Pick(row.HeaderAr, row.HeaderEn) ?? string.Empty,
                Content = LocalizedText.Pick(row.ContentAr, row.ContentEn) ?? string.Empty,
                RedirectUrl = row.RedirectUrl,
                EntityType = row.EntityType,
                EntityId = row.EntityId,
                ActorEmployeeId = row.ActorEmployeeId,
                ActorDisplayName = row.ActorDisplayName,
                ActorProfilePictureFileId = row.ActorProfilePictureFileId,
                IsSeen = row.IsSeen,
                CreatedDate = row.CreatedDate,
            })
            .ToList();

        return new CursorPagedResult<NotificationDto>(items, page.NextCursor, page.HasMore);
    }

    /// <summary>Both languages as they come out of the database, before one is chosen.</summary>
    public sealed class Row
    {
        public Guid Id { get; init; }
        public Guid NotificationId { get; init; }
        public int Type { get; init; }
        public string? HeaderAr { get; init; }
        public string? HeaderEn { get; init; }
        public string? ContentAr { get; init; }
        public string? ContentEn { get; init; }
        public string? RedirectUrl { get; init; }
        public int EntityType { get; init; }
        public Guid? EntityId { get; init; }
        public Guid? ActorEmployeeId { get; init; }
        public string? ActorDisplayName { get; init; }
        public Guid? ActorProfilePictureFileId { get; init; }
        public bool IsSeen { get; init; }
        public DateTime CreatedDate { get; init; }
    }
}
