using MediatR;
using Workvivo.API.Authorization;
using Workvivo.API.Extensions;
using Workvivo.Application.Bases;
using Workvivo.Application.Features.Auth.Commands.ChangePassword;
using Workvivo.Application.Features.Auth.Commands.Login;
using Workvivo.Application.Features.Auth.Commands.Logout;
using Workvivo.Application.Features.Auth.Commands.Refresh;
using Workvivo.Application.Features.Auth.Queries.GetCurrentSession;
using Workvivo.Domain.Models.Auth;

namespace Workvivo.API.Controllers.Auth;

/// <summary>
/// Sign-in, refresh and sign-out.
///
/// Thin by design: bind, dispatch, and move the refresh token between the cookie and
/// the command. Every rule lives in the handlers.
/// </summary>
[EnableRateLimiting(ConfigureRateLimiting.AuthPolicy)]
public class AuthController : ApiControllersBase
{
    private readonly ISender _sender;
    private readonly IHostEnvironment _environment;

    public AuthController(ISender sender, IHostEnvironment environment)
    {
        _sender = sender;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpPost]
    [Route(RouteClass.Auth.Login)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(IssueSession(result));
    }

    /// <summary>
    /// Exchanges the refresh cookie for a new access token.
    ///
    /// Anonymous on purpose - it is called precisely when the access token has expired,
    /// so requiring one would make it unreachable. The cookie is the credential.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [Route(RouteClass.Auth.Refresh)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(HttpContext, _environment);

        if (string.IsNullOrEmpty(refreshToken))
        {
            // No cookie at all is an ordinary signed-out visitor, not an error worth
            // a stack trace.
            return Unauthorized(ApiResponse<object>.Fail("Your session has expired. Please sign in again."));
        }

        var result = await _sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
        return Ok(IssueSession(result));
    }

    [AllowAnonymous]
    [HttpPost]
    [Route(RouteClass.Auth.Logout)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(HttpContext, _environment);

        await _sender.Send(new LogoutCommand(refreshToken, AllDevices: false), cancellationToken);

        // Cleared even when nothing was revoked, so a stale cookie cannot linger in the
        // browser after the server has forgotten it.
        RefreshTokenCookie.Clear(HttpContext, _environment);

        return Ok(ApiResponse.Ok("Signed out."));
    }

    [HttpPost]
    [Route(RouteClass.Auth.LogoutAll)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await _sender.Send(new LogoutCommand(null, AllDevices: true), cancellationToken);
        RefreshTokenCookie.Clear(HttpContext, _environment);

        return Ok(ApiResponse.Ok("Signed out on all devices."));
    }

    [HttpGet]
    [Route(RouteClass.Auth.Me)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var session = await _sender.Send(new GetCurrentSessionQuery(), cancellationToken);
        return Ok(ApiResponse<AuthenticatedUser>.Ok(session));
    }

    [HttpPost]
    [Route(RouteClass.Auth.ChangePassword)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);

        // Every session was just revoked, including this one, so the browser's cookie
        // is now dead and should go with it.
        RefreshTokenCookie.Clear(HttpContext, _environment);

        return Ok(ApiResponse.Ok("Password changed. Please sign in again."));
    }

    /// <summary>
    /// Puts the refresh token in the cookie and returns everything else.
    ///
    /// The refresh token is deliberately absent from the response body: putting it
    /// there would hand it straight back to the JavaScript the HttpOnly cookie exists
    /// to keep it away from.
    /// </summary>
    private ApiResponse<SessionResponse> IssueSession(AuthResult result)
    {
        RefreshTokenCookie.Write(
            HttpContext, _environment, result.RefreshToken, result.RefreshTokenExpiresAt);

        return ApiResponse<SessionResponse>.Ok(new SessionResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAt = result.AccessTokenExpiresAt,
            User = result.User,
        });
    }
}

/// <summary>What the client receives on sign-in and refresh.</summary>
public sealed class SessionResponse
{
    public required string AccessToken { get; init; }

    public required DateTime ExpiresAt { get; init; }

    public required AuthenticatedUser User { get; init; }
}
