using MediatR;
using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Application.Features.Auth.Commands.Logout;

/// <summary>Ends the current session, or every session for the user.</summary>
public sealed record LogoutCommand(string? RefreshToken, bool AllDevices) : ICommand;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenService _refreshTokens;
    private readonly ICurrentUser _currentUser;

    public LogoutCommandHandler(IRefreshTokenService refreshTokens, ICurrentUser currentUser)
    {
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (request.AllDevices && _currentUser.UserId is { } userId)
        {
            await _refreshTokens.RevokeAllForUserAsync(userId, "Signed out everywhere", cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            // The whole family, not just this token: the descendants of one sign-in are
            // that session, and leaving them live would make signing out cosmetic.
            await _refreshTokens.RevokeFamilyAsync(
                request.RefreshToken, "Signed out", _currentUser.IpAddress, cancellationToken);
        }
    }
}
