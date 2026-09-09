using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Infrastructure.DBContext;

namespace Workvivo.Infrastructure.Identity;

/// <summary>
/// Refresh tokens: issued per sign-in, rotated on every use, revoked as a family when
/// a used token is presented again.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    /// <summary>
    /// 256 bits from a cryptographic RNG. The token is a bearer credential with a
    /// two-week life, so it has to be unguessable; Guid.NewGuid() would not be, since
    /// only 122 of its bits are random and its generation is not a security primitive.
    /// </summary>
    private const int TokenBytes = 32;

    private readonly Workvivo_DbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly JwtSettings _settings;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        Workvivo_DbContext context,
        IDateTimeProvider clock,
        IOptions<JwtSettings> settings,
        ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _clock = clock;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<(string RawToken, DateTime ExpiresAt)> IssueAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        // A fresh sign-in starts its own family, so revoking one compromised session
        // does not sign the user out of their other devices.
        return await CreateAsync(userId, Guid.NewGuid(), ipAddress, userAgent, cancellationToken);
    }

    public async Task<RefreshRotationResult> RotateAsync(
        string rawToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return RefreshRotationResult.Failed("No refresh token was presented.");
        }

        var hash = Hash(rawToken);

        var existing = await _context.Security_RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token_Hash == hash, cancellationToken);

        if (existing is null)
        {
            // Never issued, or purged long ago. Nothing to revoke and nothing to learn.
            return RefreshRotationResult.Failed("The refresh token is not recognised.");
        }

        var now = _clock.UtcNow;

        if (existing.Revoked_At is not null)
        {
            // The critical branch.
            //
            // Rotation means a token is used exactly once. Seeing a revoked one again
            // means two parties hold it: the legitimate user and whoever copied it.
            // There is no way to tell which one is asking, so the entire family is
            // revoked - the real user signs in again, the attacker is locked out. A
            // system that only rotated, without this, would hand the attacker a fresh
            // token and never notice.
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId} (family {FamilyId}) from {IpAddress}. Revoking the family.",
                existing.User_Id,
                existing.Family_Id,
                ipAddress);

            await RevokeFamilyInternalAsync(
                existing.Family_Id, "Reuse detected", ipAddress, cancellationToken);

            return RefreshRotationResult.Replay();
        }

        if (existing.Expires_At <= now)
        {
            return RefreshRotationResult.Failed("The refresh token has expired.");
        }

        var user = existing.User;

        if (user is null || !user.Is_Active || user.Is_Deleted)
        {
            // Deactivating an account has to end its sessions, not just stop new
            // sign-ins.
            await RevokeFamilyInternalAsync(
                existing.Family_Id, "Account is not active", ipAddress, cancellationToken);

            return RefreshRotationResult.Failed("The account is no longer active.");
        }

        // Issuing the replacement and retiring the predecessor are one change, committed
        // together. Two separate saves could leave a live token that was never handed
        // to anybody, or retire the old one without a successor and sign the user out.
        var (replacement, newRawToken) = BuildToken(
            existing.User_Id, existing.Family_Id, ipAddress, userAgent);

        _context.Security_RefreshTokens.Add(replacement);

        existing.Revoked_At = now;
        existing.Revoked_Reason = "Rotated";
        existing.Revoked_Ip = ipAddress;
        existing.Replaced_By_Id = replacement.Id;

        await _context.SaveChangesAsync(cancellationToken);

        return RefreshRotationResult.Success(user, newRawToken, replacement.Expires_At);
    }

    public async Task RevokeFamilyAsync(
        string rawToken,
        string reason,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return;
        }

        var hash = Hash(rawToken);

        var familyId = await _context.Security_RefreshTokens
            .Where(token => token.Token_Hash == hash)
            .Select(token => (Guid?)token.Family_Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (familyId is not null)
        {
            await RevokeFamilyInternalAsync(familyId.Value, reason, ipAddress, cancellationToken);
        }
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        var affected = await _context.Security_RefreshTokens
            .Where(token => token.User_Id == userId && token.Revoked_At == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.Revoked_At, now)
                    .SetProperty(token => token.Revoked_Reason, reason),
                cancellationToken);

        _logger.LogInformation(
            "Revoked {Count} refresh tokens for user {UserId}: {Reason}", affected, userId, reason);
    }

    public async Task<int> PurgeExpiredAsync(
        TimeSpan retention,
        CancellationToken cancellationToken = default)
    {
        // Expired tokens are kept for a while after they lapse: a replay attempt
        // against a recently expired token is worth seeing in the audit trail, and it
        // cannot be recognised once the row is gone.
        var cutoff = _clock.UtcNow - retention;

        return await _context.Security_RefreshTokens
            .Where(token => token.Expires_At < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<(string RawToken, DateTime ExpiresAt)> CreateAsync(
        Guid userId,
        Guid familyId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var (token, rawToken) = BuildToken(userId, familyId, ipAddress, userAgent);

        _context.Security_RefreshTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);

        return (rawToken, token.Expires_At);
    }

    /// <summary>
    /// Builds a token entity and its raw value without saving, so a caller can commit
    /// it alongside other changes.
    ///
    /// The id is generated here rather than by the database, which is what lets the
    /// predecessor's Replaced_By_Id be set in the same unit of work instead of needing
    /// a round trip to discover it.
    /// </summary>
    private (RefreshToken Token, string RawToken) BuildToken(
        Guid userId,
        Guid familyId,
        string? ipAddress,
        string? userAgent)
    {
        var now = _clock.UtcNow;
        var rawToken = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            User_Id = userId,
            Family_Id = familyId,

            // Only the hash is stored. A database leak then yields nothing usable -
            // the same reasoning as password hashing, without the need for a slow KDF
            // because the token is already 256 bits of entropy and not guessable.
            Token_Hash = Hash(rawToken),

            Created_At = now,
            Expires_At = now.AddDays(_settings.RefreshTokenDays),
            Created_Ip = ipAddress,
            Created_User_Agent = Truncate(userAgent, 512),
            Is_Deleted = false,
        };

        return (token, rawToken);
    }

    private async Task RevokeFamilyInternalAsync(
        Guid familyId,
        string reason,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        await _context.Security_RefreshTokens
            .Where(token => token.Family_Id == familyId && token.Revoked_At == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.Revoked_At, now)
                    .SetProperty(token => token.Revoked_Reason, reason)
                    .SetProperty(token => token.Revoked_Ip, ipAddress),
                cancellationToken);
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
