using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Features.Auth.Services;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Models.Auth;

namespace Workvivo.Application.Features.Auth.Services;

public sealed class AuthSessionFactory : IAuthSessionFactory
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUser _currentUser;

    public AuthSessionFactory(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IRefreshTokenService refreshTokens,
        IPermissionService permissions,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<AuthResult> CreateForSignInAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        var (refreshToken, refreshExpiresAt) = await _refreshTokens.IssueAsync(
            user.Id, _currentUser.IpAddress, _currentUser.UserAgent, cancellationToken);

        return await BuildAsync(user, refreshToken, refreshExpiresAt, cancellationToken);
    }

    public Task<AuthResult> CreateForRefreshAsync(
        ApplicationUser user,
        string rotatedRefreshToken,
        DateTime refreshTokenExpiresAt,
        CancellationToken cancellationToken = default)
    {
        // The token rotation already produced is carried through unchanged. Issuing
        // another here is what previously broke the family chain.
        return BuildAsync(user, rotatedRefreshToken, refreshTokenExpiresAt, cancellationToken);
    }

    private async Task<AuthResult> BuildAsync(
        ApplicationUser user,
        string refreshToken,
        DateTime refreshExpiresAt,
        CancellationToken cancellationToken)
    {
        // Projected, not loaded: only four fields of the employee are needed, and
        // materialising the aggregate would drag its navigations in behind it.
        var profile = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.User_Id == user.Id)
            .Select(employee => new
            {
                employee.Id,
                employee.Display_Name,
                employee.Preferred_Language,
                employee.Profile_Picture_File_Id,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var permissions = await _permissions.GetPermissionsAsync(user.Id, cancellationToken);
        var roles = await _unitOfWork.UserManager.GetRolesAsync(user);

        var (accessToken, accessExpiresAt) = _tokenService.CreateAccessToken(user, profile?.Id);

        return new AuthResult
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = new AuthenticatedUser
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                DisplayName = profile?.Display_Name ?? user.Full_Name_En ?? user.Full_Name_Ar,
                Email = user.Email,
                EmployeeId = profile?.Id,
                IsAdmin = user.Is_Admin,
                UserType = user.User_Type,
                PreferredLanguage = profile?.Preferred_Language ?? "en",
                ProfilePictureFileId = profile?.Profile_Picture_File_Id,
                Permissions = [.. permissions.OrderBy(permission => permission, StringComparer.Ordinal)],
                Roles = [.. roles],
            },
        };
    }
}
