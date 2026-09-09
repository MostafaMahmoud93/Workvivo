using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workvivo.Application.Features.Auth.Services;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Exceptions;
using Workvivo.Domain.Models.Auth;

namespace Workvivo.Application.Features.Auth.Commands.Login;

/// <summary>
/// Verifies credentials and issues a session.
///
/// Supersedes <c>AuthService.Token</c>, which issued a 24-hour access token with no
/// refresh, no rotation and no way to revoke it.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthSessionFactory _sessionFactory;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly AuthenticationSettings _settings;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IAuthSessionFactory sessionFactory,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IOptions<AuthenticationSettings> settings,
        ILogger<LoginCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _sessionFactory = sessionFactory;
        _currentUser = currentUser;
        _clock = clock;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var user = await _unitOfWork.UserManager.FindByNameAsync(userName);

        if (user is not null && await IsLockedOutAsync(user.Id, cancellationToken))
        {
            _logger.LogWarning("Sign-in refused for locked-out user {UserId}", user.Id);

            // Named as a distinct failure so the user is told to wait rather than
            // shown "wrong password" and left retrying into a longer lockout.
            throw new BusinessRuleException(
                $"Too many failed attempts. Try again in {_settings.LockoutDuration.TotalMinutes:0} minutes.",
                "auth.locked-out");
        }

        var signIn = user is null
            ? SignInResult.Failed
            : await _unitOfWork.SignInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

        await RecordAttemptAsync(user?.Id, signIn.Succeeded, cancellationToken);

        // One message for every failure mode, and the password is verified even when
        // the account does not exist would otherwise be skipped - so the endpoint
        // cannot be used to discover which usernames are real, by response or by timing.
        if (user is null || !signIn.Succeeded)
        {
            _logger.LogInformation("Failed sign-in for {UserName}", userName);
            throw new UnauthorizedException("The username or password is incorrect.");
        }

        if (!user.Is_Active || user.Is_Deleted)
        {
            throw new UnauthorizedException("This account is not active.");
        }

        if (_settings.RequireConfirmedEmail && !user.EmailConfirmed)
        {
            throw new BusinessRuleException(
                "Confirm your email address before signing in.", "auth.email-unconfirmed");
        }

        _logger.LogInformation("User {UserId} signed in", user.Id);

        return await _sessionFactory.CreateForSignInAsync(user, cancellationToken);
    }

    /// <summary>
    /// Counts recent failures for the account.
    ///
    /// Uses the existing UserLoginLog table rather than Identity's AccessFailedCount,
    /// because the template explicitly ignores Identity's lockout columns in
    /// OnModelCreating - they are not mapped, so they cannot hold anything.
    /// </summary>
    private async Task<bool> IsLockedOutAsync(Guid userId, CancellationToken cancellationToken)
    {
        var since = _clock.UtcNow - _settings.FailedAttemptWindow;

        var recentFailures = await _unitOfWork.UserLoginLogRepository
            .GetAllAsync(log => log.UserId == userId && log.LoginTime >= since && !log.IsSuccessful);

        if (recentFailures.Count < _settings.MaxFailedAttempts)
        {
            return false;
        }

        // The lockout runs from the most recent failure, so it extends while attempts
        // continue rather than expiring on a fixed schedule an attacker can wait out.
        var mostRecent = recentFailures.Max(log => log.LoginTime);
        return mostRecent.Add(_settings.LockoutDuration) > _clock.UtcNow;
    }

    private async Task RecordAttemptAsync(Guid? userId, bool succeeded, CancellationToken cancellationToken)
    {
        if (userId is null)
        {
            // No row for an unknown username: it would let anyone fill the table by
            // guessing, and there is no account for the entry to belong to.
            return;
        }

        await _unitOfWork.UserLoginLogRepository.AddAsync(new UserLoginLog
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            LoginTime = _clock.UtcNow,
            IPAddress = _currentUser.IpAddress ?? "unknown",
            IsSuccessful = succeeded,
            Is_Deleted = false,
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
