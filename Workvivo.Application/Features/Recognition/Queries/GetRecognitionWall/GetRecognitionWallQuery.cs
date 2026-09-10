using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Recognition.Dtos;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Application.Features.Recognition.Queries.GetRecognitionWall;

/// <summary>
/// Recognition, newest first - the whole wall, or one person's.
///
/// Private recognition never appears here for anybody except the two people involved.
/// That is the whole point of the setting, and it is enforced in the query rather
/// than filtered afterwards, so a paging bug cannot leak one.
/// </summary>
public sealed class GetRecognitionWallQuery : CursorRequest, IQuery<CursorPagedResult<RecognitionDto>>
{
    /// <summary>Limits the wall to recognition this person received. Null for everyone.</summary>
    public Guid? RecipientEmployeeId { get; set; }
}

public sealed class GetRecognitionWallQueryHandler
    : IRequestHandler<GetRecognitionWallQuery, CursorPagedResult<RecognitionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentEmployee _currentEmployee;

    public GetRecognitionWallQueryHandler(IUnitOfWork unitOfWork, CurrentEmployee currentEmployee)
    {
        _unitOfWork = unitOfWork;
        _currentEmployee = currentEmployee;
    }

    public async Task<CursorPagedResult<RecognitionDto>> Handle(
        GetRecognitionWallQuery request,
        CancellationToken cancellationToken)
    {
        var viewerId = await _currentEmployee.RequireIdAsync(cancellationToken);

        var viewerDepartmentId = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Id == viewerId)
            .Select(employee => employee.Department_Id)
            .FirstOrDefaultAsync(cancellationToken);

        var query = _unitOfWork.Repository<Domain.Entities.Recognition.Recognition, Guid>()
            .GetAllQ()

            // The visibility rule, applied in SQL rather than filtered afterwards, so
            // a paging bug cannot leak one.
            //
            // All three levels are honoured here. An earlier version only enforced
            // Private and let Department-scoped recognition onto the company-wide
            // wall on the grounds that its feed post was targeted - which was wrong:
            // the wall is a second way to read the same thing, and "visible to their
            // department" has to mean that everywhere or it means nothing.
            //
            // The two people involved always see their own, whatever the setting.
            .Where(recognition =>
                recognition.Sender_Employee_Id == viewerId
                || recognition.Recipient_Employee_Id == viewerId
                || recognition.Visibility == RecognitionVisibility.Public
                || (recognition.Visibility == RecognitionVisibility.Department
                    && viewerDepartmentId != null
                    && recognition.Recipient!.Department_Id == viewerDepartmentId));

        if (request.RecipientEmployeeId is { } recipientId)
        {
            query = query.Where(recognition => recognition.Recipient_Employee_Id == recipientId);
        }

        if (Cursor.TryDecode(request.Cursor, out var cursorDate, out var cursorId))
        {
            query = query.Where(recognition =>
                recognition.Recognised_On < cursorDate
                || (recognition.Recognised_On == cursorDate && recognition.Id.CompareTo(cursorId) < 0));
        }

        return await query
            .OrderByDescending(recognition => recognition.Recognised_On)
            .ThenByDescending(recognition => recognition.Id)
            .Select(Projection)
            .ToCursorPagedResultAsync(
                request.PageSize,
                row => (row.RecognisedOn, row.Id),
                cancellationToken);
    }

    /// <summary>An expression, so EF translates it instead of lazy-loading four navigations per row.</summary>
    private static readonly System.Linq.Expressions.Expression<
        Func<Domain.Entities.Recognition.Recognition, RecognitionDto>> Projection =
        recognition => new RecognitionDto
        {
            Id = recognition.Id,
            SenderEmployeeId = recognition.Sender_Employee_Id,
            SenderDisplayName = recognition.Sender!.Display_Name,
            RecipientEmployeeId = recognition.Recipient_Employee_Id,
            RecipientDisplayName = recognition.Recipient!.Display_Name,
            RecognitionTypeId = recognition.Recognition_Type_Id,
            RecognitionTypeName =
                recognition.RecognitionType!.Name_En ?? recognition.RecognitionType.Name_Ar,
            BadgeIcon = recognition.RecognitionType.Badge_Icon,
            BadgeColor = recognition.RecognitionType.Badge_Color,
            Message = recognition.Message,
            Points = recognition.Points,
            Visibility = (int)recognition.Visibility,
            RecognisedOn = recognition.Recognised_On,
        };
}
