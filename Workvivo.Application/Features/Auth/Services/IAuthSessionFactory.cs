using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Models.Auth;

namespace Workvivo.Application.Features.Auth.Services;

/// <summary>
/// Assembles a signed-in session: access token, refresh token, and the profile and
/// permissions the client needs.
///
/// Sign-in and refresh differ in exactly one respect - where the refresh token comes
/// from - so they are separate methods rather than one with a flag. An earlier single
/// method always issued a new token, which silently broke refresh-token families:
/// rotation produced a successor, the factory then ignored it and started a new family,
/// and revoking a compromised family no longer touched the token the client held.
/// </summary>
public interface IAuthSessionFactory
{
    /// <summary>A fresh sign-in. Starts a new refresh-token family.</summary>
    Task<AuthResult> CreateForSignInAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// A refreshed session, carrying the token rotation already produced so it stays in
    /// the same family.
    /// </summary>
    Task<AuthResult> CreateForRefreshAsync(
        ApplicationUser user,
        string rotatedRefreshToken,
        DateTime refreshTokenExpiresAt,
        CancellationToken cancellationToken = default);
}
