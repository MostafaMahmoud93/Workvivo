using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Common.Security;
using Workvivo.Application.Features.Communities.Common;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Communities.Commands.SaveCommunity;

/// <summary>Creates a community, or updates one when <see cref="Id"/> is supplied.</summary>
public sealed record SaveCommunityCommand(
    Guid? Id,
    string NameAr,
    string? NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    CommunityPrivacy Privacy) : ICommand<Guid>;

public sealed class SaveCommunityCommandValidator : AbstractValidator<SaveCommunityCommand>
{
    public SaveCommunityCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).MaximumLength(200);
        RuleFor(x => x.DescriptionAr).MaximumLength(1000);
        RuleFor(x => x.DescriptionEn).MaximumLength(1000);
        RuleFor(x => x.Privacy).Must(Enum.IsDefined).WithMessage("Unknown privacy setting.");
    }
}

public sealed class SaveCommunityCommandHandler : IRequestHandler<SaveCommunityCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CommunityAuthorization _authorization;
    private readonly CurrentEmployee _currentEmployee;
    private readonly IPermissionService _permissions;
    private readonly IAudienceResolver _audience;
    private readonly IDateTimeProvider _clock;

    public SaveCommunityCommandHandler(
        IUnitOfWork unitOfWork,
        CommunityAuthorization authorization,
        CurrentEmployee currentEmployee,
        IPermissionService permissions,
        IAudienceResolver audience,
        IDateTimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _authorization = authorization;
        _currentEmployee = currentEmployee;
        _permissions = permissions;
        _audience = audience;
        _clock = clock;
    }

    public async Task<Guid> Handle(SaveCommunityCommand request, CancellationToken cancellationToken)
    {
        return request.Id is { } id
            ? await UpdateAsync(id, request, cancellationToken)
            : await CreateAsync(request, cancellationToken);
    }

    private async Task<Guid> CreateAsync(SaveCommunityCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployee.RequireIdAsync(cancellationToken);

        // Creating a community is not something every employee may do. Left open, an
        // organisation of any size accumulates hundreds of abandoned half-groups and
        // the directory stops being useful.
        if (!await _permissions.HasPermissionAsync(
                _currentEmployee.UserId, PermissionKeys.CommunityManage, cancellationToken))
        {
            throw new ForbiddenException(
                "You cannot create communities.", PermissionKeys.CommunityManage);
        }

        var now = _clock.UtcNow;

        var community = new Community
        {
            Id = Guid.NewGuid(),
            Slug = await UniqueSlugAsync(request.NameEn, request.NameAr, null, cancellationToken),
            Name_Ar = request.NameAr.Trim(),
            Name_En = request.NameEn?.Trim(),
            Description_Ar = request.DescriptionAr?.Trim(),
            Description_En = request.DescriptionEn?.Trim(),
            Privacy = request.Privacy,
            Owner_Employee_Id = employeeId,
            Is_Active = true,

            // The owner counts as the first member, so the counter starts at one rather
            // than at zero with a member row that nothing incremented for.
            Members_Count = 1,
            Is_Deleted = false,
        };

        await _unitOfWork.Repository<Community, Guid>().AddAsync(community);

        // The owner is also a member row. The column answers "who owns this" in one
        // read; the row keeps them inside the same membership queries as everyone
        // else, including the audience key set that decides what they see.
        await _unitOfWork.Repository<CommunityMember, Guid>().AddAsync(new CommunityMember
        {
            Id = Guid.NewGuid(),
            Community_Id = community.Id,
            Employee_Id = employeeId,
            Member_Role = CommunityMemberRole.Owner,
            Membership_Status = MembershipStatus.Approved,
            Requested_At = now,
            Joined_At = now,
            Is_Deleted = false,
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The owner's audience key set now includes this community. Without this they
        // would not see their own community's posts for up to ten minutes.
        await _audience.InvalidateAsync(employeeId, cancellationToken);

        return community.Id;
    }

    private async Task<Guid> UpdateAsync(
        Guid id,
        SaveCommunityCommand request,
        CancellationToken cancellationToken)
    {
        var access = await _authorization.RequireManageableAsync(id, cancellationToken);
        var community = access.Community;

        community.Name_Ar = request.NameAr.Trim();
        community.Name_En = request.NameEn?.Trim();
        community.Description_Ar = request.DescriptionAr?.Trim();
        community.Description_En = request.DescriptionEn?.Trim();

        // The slug is deliberately not regenerated on rename. It is the community's
        // permanent link; changing it silently breaks every message, bookmark and
        // document that ever pointed at the group.
        community.Privacy = request.Privacy;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return community.Id;
    }

    /// <summary>
    /// A slug nothing else is using.
    ///
    /// The unique index is what actually guarantees it; this avoids losing the write
    /// to a constraint violation on the common case of two "Football Club" groups.
    /// </summary>
    private async Task<string> UniqueSlugAsync(
        string? nameEn,
        string nameAr,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        var communities = _unitOfWork.Repository<Community, Guid>();
        var candidate = CommunitySlug.From(nameEn, nameAr);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var slug = candidate;

            var taken = await communities.GetAllQ()
                .AnyAsync(
                    community => community.Slug == slug && (excludingId == null || community.Id != excludingId),
                    cancellationToken);

            if (!taken)
            {
                return slug;
            }

            candidate = CommunitySlug.From(nameEn, nameAr, Guid.NewGuid().ToString("N")[..6]);
        }

        // Five collisions on a random six-character suffix means something is wrong
        // rather than unlucky, and looping forever would be worse than failing.
        throw new BusinessRuleException(
            "Could not allocate a unique link for this community.", "community.slug-exhausted");
    }
}
