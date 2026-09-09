using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>Issues signed access tokens.</summary>
public interface ITokenService
{
    /// <summary>
    /// Mints a short-lived JWT for the user.
    ///
    /// Permissions are deliberately not claims. A token cannot be recalled once issued,
    /// so a permission baked into one keeps working until it expires; resolving them
    /// per request bounds a revocation to the permission cache instead. It also keeps
    /// the token small, which matters when it rides on every request.
    /// </summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user, Guid? employeeId);
}
