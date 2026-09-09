using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Exceptions;
using Workvivo.Domain.Models.Auth;

namespace Workvivo.Application.Features.Auth.Queries.GetCurrentSession;

/// <summary>
/// The signed-in user's profile and current permissions.
///
/// The client calls this on start-up rather than decoding its access token, because
/// permissions are not in the token - they are resolved server-side so that a
/// revocation takes effect without waiting for the token to expire.
/// </summary>
public sealed record GetCurrentSessionQuery : IQuery<AuthenticatedUser>;

public sealed class GetCurrentSessionQueryHandler : IRequestHandler<GetCurrentSessionQuery, AuthenticatedUser>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUser _currentUser;

    public GetCurrentSessionQueryHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissions,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<AuthenticatedUser> Handle(GetCurrentSessionQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedException();

        var user = await _unitOfWork.UserManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedException();

        var employee = await _unitOfWork.Repository<Employee, Guid>()
            .FirstOrDefaultAsync(candidate => candidate.User_Id == userId);

        var permissions = await _permissions.GetPermissionsAsync(userId, cancellationToken);
        var roles = await _unitOfWork.UserManager.GetRolesAsync(user);

        return new AuthenticatedUser
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            DisplayName = employee?.Display_Name ?? user.Full_Name_En ?? user.Full_Name_Ar,
            Email = user.Email,
            EmployeeId = employee?.Id,
            IsAdmin = user.Is_Admin,
            UserType = user.User_Type,
            PreferredLanguage = employee?.Preferred_Language ?? "en",
            ProfilePictureFileId = employee?.Profile_Picture_File_Id,
            Permissions = [.. permissions.OrderBy(permission => permission, StringComparer.Ordinal)],
            Roles = [.. roles],
        };
    }
}
