using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Issues, rotates and revokes refresh tokens.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Starts a new token family for a fresh sign-in and returns the raw token. The raw
    /// value exists only in this return - the store keeps a hash.
    /// </summary>
    Task<(string RawToken, DateTime ExpiresAt)> IssueAsync(
        Guid userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a refresh token for a new one, revoking the old.
    ///
    /// Presenting a token that has already been revoked means two parties hold the same
    /// credential - the legitimate user and somebody who copied it. The whole family is
    /// revoked in that case, which ends both sessions; the real user signs in again,
    /// the attacker cannot.
    /// </summary>
    Task<RefreshRotationResult> RotateAsync(
        string rawToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

    /// <summary>Revokes one token and everything descended from the same sign-in.</summary>
    Task RevokeFamilyAsync(string rawToken, string reason, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Revokes every live token for a user - "sign out on all devices".</summary>
    Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Removes tokens that expired long enough ago to be of no forensic use.</summary>
    Task<int> PurgeExpiredAsync(TimeSpan retention, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of a rotation attempt.</summary>
public sealed class RefreshRotationResult
{
    private RefreshRotationResult()
    {
    }

    public bool Succeeded { get; private init; }

    /// <summary>True when a revoked token was presented - a replay. The family is revoked.</summary>
    public bool ReuseDetected { get; private init; }

    public ApplicationUser? User { get; private init; }

    public string? RawToken { get; private init; }

    public DateTime ExpiresAt { get; private init; }

    public string? FailureReason { get; private init; }

    public static RefreshRotationResult Success(ApplicationUser user, string rawToken, DateTime expiresAt) =>
        new() { Succeeded = true, User = user, RawToken = rawToken, ExpiresAt = expiresAt };

    public static RefreshRotationResult Failed(string reason) =>
        new() { Succeeded = false, FailureReason = reason };

    public static RefreshRotationResult Replay() =>
        new() { Succeeded = false, ReuseDetected = true, FailureReason = "Refresh token reuse detected." };
}
