using MediatR;
using Workvivo.Application.Bases;
using Workvivo.Application.Common.Paging;
using Workvivo.Application.Features.Communities.Commands.InviteToCommunity;
using Workvivo.Application.Features.Communities.Commands.JoinCommunity;
using Workvivo.Application.Features.Communities.Commands.LeaveCommunity;
using Workvivo.Application.Features.Communities.Commands.RespondToInvitation;
using Workvivo.Application.Features.Communities.Commands.ReviewMembership;
using Workvivo.Application.Features.Communities.Commands.SaveCommunity;
using Workvivo.Application.Features.Communities.Commands.SetMemberRole;
using Workvivo.Application.Features.Communities.Dtos;
using Workvivo.Application.Features.Communities.Queries.GetCommunities;
using Workvivo.Application.Features.Communities.Queries.GetCommunity;
using Workvivo.Application.Features.Communities.Queries.GetCommunityMembers;
using Workvivo.Application.Features.Communities.Queries.GetMyInvitations;
using Workvivo.Domain.Abstractions.Enums;

namespace Workvivo.API.Controllers.Platform;

/// <summary>
/// Communities and their membership.
///
/// Only creating one is gated by a platform permission. Everything else is decided by
/// the community itself - its privacy setting and the caller's membership row - which
/// an endpoint attribute cannot express, so those checks live in
/// <c>CommunityAuthorization</c> where they can see both.
/// </summary>
public class CommunitiesController : ApiControllersBase
{
    private readonly ISender _sender;

    public CommunitiesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Route(RouteClass.Communities.List)]
    public async Task<IActionResult> List(
        [FromQuery] GetCommunitiesQuery query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<CommunitySummaryDto>>.Ok(await _sender.Send(query, cancellationToken)));

    [HttpGet]
    [Route(RouteClass.Communities.Detail)]
    public async Task<IActionResult> Detail(Guid communityId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CommunityDetailDto>.Ok(
            await _sender.Send(new GetCommunityQuery(communityId), cancellationToken)));

    [HttpPost]
    [Route(RouteClass.Communities.Save)]
    public async Task<IActionResult> Save(
        SaveCommunityCommand command,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<Guid>.Ok(await _sender.Send(command, cancellationToken)));

    [HttpGet]
    [Route(RouteClass.Communities.Members)]
    public async Task<IActionResult> Members(
        Guid communityId,
        [FromQuery] GetCommunityMembersQuery query,
        CancellationToken cancellationToken)
    {
        // Taken from the route so the two cannot disagree.
        query.CommunityId = communityId;

        return Ok(ApiResponse<PagedResult<CommunityMemberDto>>.Ok(
            await _sender.Send(query, cancellationToken)));
    }

    /// <summary>
    /// Joins, or asks to join. The response says which happened - the client cannot
    /// know without the community's privacy setting.
    /// </summary>
    [HttpPost]
    [Route(RouteClass.Communities.Join)]
    public async Task<IActionResult> Join(Guid communityId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<int>.Ok(await _sender.Send(new JoinCommunityCommand(communityId), cancellationToken)));

    [HttpDelete]
    [Route(RouteClass.Communities.Leave)]
    public async Task<IActionResult> Leave(Guid communityId, CancellationToken cancellationToken)
    {
        await _sender.Send(new LeaveCommunityCommand(communityId), cancellationToken);
        return Ok(ApiResponse.Ok("You have left the community."));
    }

    [HttpPost]
    [Route(RouteClass.Communities.Review)]
    public async Task<IActionResult> Review(
        Guid communityId,
        Guid employeeId,
        [FromBody] ReviewRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new ReviewMembershipCommand(communityId, employeeId, request.Decision), cancellationToken);

        return Ok(ApiResponse.Ok("Membership updated."));
    }

    [HttpPut]
    [Route(RouteClass.Communities.Role)]
    public async Task<IActionResult> Role(
        Guid communityId,
        Guid employeeId,
        [FromBody] RoleRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new SetMemberRoleCommand(communityId, employeeId, request.Role), cancellationToken);

        return Ok(ApiResponse.Ok("Role updated."));
    }

    [HttpPost]
    [Route(RouteClass.Communities.Invite)]
    public async Task<IActionResult> Invite(
        Guid communityId,
        [FromBody] InviteRequest request,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse<int>.Ok(await _sender.Send(
            new InviteToCommunityCommand(communityId, request.EmployeeIds ?? [], request.Message),
            cancellationToken)));

    [HttpGet]
    [Route(RouteClass.Communities.MyInvitations)]
    public async Task<IActionResult> MyInvitations(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<CommunityInvitationDto>>.Ok(
            await _sender.Send(new GetMyInvitationsQuery(), cancellationToken)));

    [HttpPost]
    [Route(RouteClass.Communities.RespondToInvitation)]
    public async Task<IActionResult> RespondToInvitation(
        Guid invitationId,
        [FromBody] InvitationResponse request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RespondToInvitationCommand(invitationId, request.Accept), cancellationToken);

        return Ok(ApiResponse.Ok(request.Accept ? "Invitation accepted." : "Invitation declined."));
    }
}

public sealed record ReviewRequest(MembershipDecision Decision);

public sealed record RoleRequest(CommunityMemberRole Role);

public sealed record InviteRequest(IReadOnlyList<Guid>? EmployeeIds, string? Message);

public sealed record InvitationResponse(bool Accept);
