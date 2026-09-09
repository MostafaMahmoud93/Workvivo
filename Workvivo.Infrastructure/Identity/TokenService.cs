using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Infrastructure.Identity;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _clock;
    private readonly SigningCredentials _credentials;

    public TokenService(IOptions<JwtSettings> settings, IDateTimeProvider clock)
    {
        _settings = settings.Value;
        _clock = clock;

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public (string Token, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user, Guid? employeeId)
    {
        var issuedAt = _clock.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),

            // A per-token identifier, so a specific token can be named in an audit
            // entry without writing the token itself anywhere.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(user.User_Type))
        {
            claims.Add(new Claim("UserTypeCode", user.User_Type));
        }

        if (user.Is_Admin)
        {
            claims.Add(new Claim("IsAdmin", "true"));
        }

        if (employeeId is not null)
        {
            // Almost every request needs the employee id - a post's author, a
            // reaction's owner - and carrying it here saves a lookup per request.
            claims.Add(new Claim("EmployeeId", employeeId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: _credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

/// <summary>
/// Token settings as Infrastructure sees them.
///
/// A separate type from the API's JwtOptions on purpose: Infrastructure must not
/// reference the web project, and the API binds and validates the configuration then
/// hands the values down.
/// </summary>
public sealed class JwtSettings
{
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}
