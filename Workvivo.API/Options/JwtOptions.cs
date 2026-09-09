using System.ComponentModel.DataAnnotations;

namespace Workvivo.API.Options;

/// <summary>
/// Token settings, bound from the "Jwt" section and validated at startup.
///
/// The signing key has no default and a minimum length that is enforced: a weak or
/// shared key means anyone who has it can mint a token for any user. It comes from
/// user-secrets in development and from the environment or a secret store everywhere
/// else, and is never written to appsettings.json.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC-SHA256 signing key. At least 32 bytes, because the algorithm's security
    /// stops improving above the hash size and falls off sharply below it.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters.")]
    public string SigningKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access-token lifetime. Short on purpose: the token cannot be revoked once
    /// issued, so its lifetime is the window an attacker gets from a stolen one.
    /// The refresh token, which can be revoked, carries the long-lived session.
    /// </summary>
    [Range(1, 120)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 14;
}
