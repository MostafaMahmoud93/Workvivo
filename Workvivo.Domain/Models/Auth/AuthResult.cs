namespace Workvivo.Domain.Models.Auth;

/// <summary>
/// What a successful sign-in or refresh hands back.
///
/// The refresh token is separated from the rest because it never reaches the response
/// body: the API writes it into an HttpOnly cookie and the client never sees it.
/// </summary>
public sealed class AuthResult
{
    public required string AccessToken { get; init; }

    public required DateTime AccessTokenExpiresAt { get; init; }

    /// <summary>Raw refresh token. Goes into an HttpOnly cookie, never into JSON.</summary>
    public required string RefreshToken { get; init; }

    public required DateTime RefreshTokenExpiresAt { get; init; }

    public required AuthenticatedUser User { get; init; }
}

/// <summary>The signed-in user as the client needs to know them.</summary>
public sealed class AuthenticatedUser
{
    public required Guid UserId { get; init; }

    public required string UserName { get; init; }

    public string? DisplayName { get; init; }

    public string? Email { get; init; }

    public Guid? EmployeeId { get; init; }

    public bool IsAdmin { get; init; }

    public string? UserType { get; init; }

    public string? PreferredLanguage { get; init; }

    public Guid? ProfilePictureFileId { get; init; }

    /// <summary>
    /// The user's permissions, for the interface to enable and disable features.
    ///
    /// Presentation only. Every one of these is checked again server-side on the actual
    /// request - a client that edits this list gets a nicer-looking menu and 403s.
    /// </summary>
    public required IReadOnlyList<string> Permissions { get; init; }

    public required IReadOnlyList<string> Roles { get; init; }
}
