using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Events;
using Workvivo.Application.Features.Notifications.Common;
using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Events;

namespace Workvivo.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Looks up the names a community notification needs.
///
/// Shared by the three handlers below rather than each repeating the same two
/// queries, and it returns null when either is missing so a notification is simply
/// not sent instead of one reading "null asked to join null".
/// </summary>
internal sealed class CommunityNotificationContext
{
    private readonly IUnitOfWork _unitOfWork;

    public CommunityNotificationContext(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(string CommunityName, string ActorName)?> LoadAsync(
        Guid communityId,
        Guid actorEmployeeId,
        CancellationToken cancellationToken)
    {
        var community = await _unitOfWork.Repository<Community, Guid>()
            .GetAllQ()
            .Where(candidate => candidate.Id == communityId)
            .Select(candidate => new { candidate.Name_Ar, candidate.Name_En })
            .FirstOrDefaultAsync(cancellationToken);

        if (community is null)
        {
            return null;
        }

        var actorName = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.Id == actorEmployeeId)
            .Select(employee => employee.Display_Name)
            .FirstOrDefaultAsync(cancellationToken);

        return actorName is null
            ? null
            : (LocalizedText.Pick(community.Name_Ar, community.Name_En) ?? string.Empty, actorName);
    }

    /// <summary>The people who can act on a join request.</summary>
    public Task<List<Guid>> ModeratorsAsync(Guid communityId, CancellationToken cancellationToken) =>
        _unitOfWork.Repository<CommunityMember, Guid>()
            .GetAllQ()
            .Where(member =>
                member.Community_Id == communityId
                && member.Membership_Status == MembershipStatus.Approved
                && (member.Member_Role == CommunityMemberRole.Moderator
                    || member.Member_Role == CommunityMemberRole.Owner))
            .Select(member => member.Employee_Id)
            .ToListAsync(cancellationToken);
}

/// <summary>Tells a community's moderators that somebody wants in.</summary>
public sealed class CommunityJoinRequestedNotificationHandler
    : INotificationHandler<DomainEventNotification<CommunityJoinRequestedDomainEvent>>
{
    private readonly CommunityNotificationContext _context;
    private readonly INotificationDispatcher _dispatcher;

    public CommunityJoinRequestedNotificationHandler(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher)
    {
        _context = new CommunityNotificationContext(unitOfWork);
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        DomainEventNotification<CommunityJoinRequestedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var raised = notification.DomainEvent;

        if (await _context.LoadAsync(raised.CommunityId, raised.RequesterEmployeeId, cancellationToken)
            is not { } context)
        {
            return;
        }

        var (communityName, actorName) = context;

        var moderators = await _context.ModeratorsAsync(raised.CommunityId, cancellationToken);

        if (moderators.Count == 0)
        {
            return;
        }

        await _dispatcher.DispatchAsync(
            new NotificationSendRequest
            {
                Type = NotificationType.CommunityJoinRequest,
                RecipientEmployeeIds = moderators,
                ActorEmployeeId = raised.RequesterEmployeeId,
                EntityType = NotificationEntityType.Community,
                EntityId = raised.CommunityId,
                Copy = NotificationCopy.AskedToJoinYourCommunity(actorName, communityName),

                // Straight to the approval queue, not the community page. A moderator
                // who has to go and find the queue is a moderator who does not.
                RedirectUrl = NotificationLinks.CommunityMembers(raised.CommunityId),
            },
            cancellationToken);
    }
}

/// <summary>Tells somebody their membership was approved.</summary>
public sealed class CommunityMembershipApprovedNotificationHandler
    : INotificationHandler<DomainEventNotification<CommunityMembershipApprovedDomainEvent>>
{
    private readonly CommunityNotificationContext _context;
    private readonly INotificationDispatcher _dispatcher;

    public CommunityMembershipApprovedNotificationHandler(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher)
    {
        _context = new CommunityNotificationContext(unitOfWork);
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        DomainEventNotification<CommunityMembershipApprovedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var raised = notification.DomainEvent;

        if (await _context.LoadAsync(raised.CommunityId, raised.ReviewerEmployeeId, cancellationToken)
            is not { } context)
        {
            return;
        }

        var communityName = context.CommunityName;

        await _dispatcher.DispatchAsync(
            new NotificationSendRequest
            {
                Type = NotificationType.CommunityMembershipApproved,
                RecipientEmployeeIds = [raised.MemberEmployeeId],
                ActorEmployeeId = raised.ReviewerEmployeeId,
                EntityType = NotificationEntityType.Community,
                EntityId = raised.CommunityId,
                Copy = NotificationCopy.YourMembershipWasApproved(communityName),
                RedirectUrl = NotificationLinks.Community(raised.CommunityId),
            },
            cancellationToken);
    }
}

/// <summary>Tells somebody they were invited.</summary>
public sealed class CommunityInvitationNotificationHandler
    : INotificationHandler<DomainEventNotification<CommunityInvitationSentDomainEvent>>
{
    private readonly CommunityNotificationContext _context;
    private readonly INotificationDispatcher _dispatcher;

    public CommunityInvitationNotificationHandler(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher)
    {
        _context = new CommunityNotificationContext(unitOfWork);
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        DomainEventNotification<CommunityInvitationSentDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var raised = notification.DomainEvent;

        if (await _context.LoadAsync(raised.CommunityId, raised.InvitedByEmployeeId, cancellationToken)
            is not { } context)
        {
            return;
        }

        var (communityName, actorName) = context;

        await _dispatcher.DispatchAsync(
            new NotificationSendRequest
            {
                Type = NotificationType.CommunityInvitation,
                RecipientEmployeeIds = [raised.InvitedEmployeeId],
                ActorEmployeeId = raised.InvitedByEmployeeId,
                EntityType = NotificationEntityType.Community,
                EntityId = raised.CommunityId,
                Copy = NotificationCopy.InvitedYouToACommunity(actorName, communityName),
                RedirectUrl = NotificationLinks.Community(raised.CommunityId),
            },
            cancellationToken);
    }
}
