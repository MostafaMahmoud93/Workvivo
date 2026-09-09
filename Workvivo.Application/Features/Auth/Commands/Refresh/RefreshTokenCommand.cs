using MediatR;
using Microsoft.Extensions.Logging;
using Workvivo.Application.Common.Messaging;
using Workvivo.Application.Features.Auth.Services;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Exceptions;
using Workvivo.Domain.Models.Auth;

namespace Workvivo.Application.Features.Auth.Commands.Refresh;

/// <summary>
/// Exchanges a refresh token for a new session.
///
/// The token arrives from an HttpOnly cookie, read by the controller - it is never a
/// field the client can populate, which is what keeps it out of reach of script on the
/// page.
///
/// Deliberately outside the pipeline transaction. Replay detection revokes the token
/// family and then rejects the request; wrapped in a transaction, the throw would roll
/// the revocation back and leave the compromised session alive. Rotation manages its
/// own atomicity with a single SaveChanges.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken)
    : ICommand<AuthResult>, INonTransactionalCommand;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IAuthSessionFactory _sessionFactory;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokens,
        IAuthSessionFactory sessionFactory,
        ICurrentUser currentUser,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokens = refreshTokens;
        _sessionFactory = sessionFactory;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _refreshTokens.RotateAsync(
            request.RefreshToken, _currentUser.IpAddress, _currentUser.UserAgent, cancellationToken);

        if (result.ReuseDetected)
        {
            // The family is already revoked by this point. The client is told only that
            // it must sign in again - naming the reason would confirm to an attacker
            // that their replay was noticed.
            _logger.LogWarning("Refresh rejected: token reuse. Correlation {CorrelationId}", _currentUser.CorrelationId);
            throw new UnauthorizedException("Your session has ended. Please sign in again.");
        }

        if (!result.Succeeded || result.User is null || result.RawToken is null)
        {
            throw new UnauthorizedException("Your session has expired. Please sign in again.");
        }

        return await _sessionFactory.CreateForRefreshAsync(
            result.User, result.RawToken, result.ExpiresAt, cancellationToken);
    }
}
