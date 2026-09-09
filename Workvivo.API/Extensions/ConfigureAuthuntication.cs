using Workvivo.API.Options;

namespace Workvivo.API.Extensions;

public static class ConfigureAuthuntication
{
    /// <summary>
    /// Path prefix the SignalR notification hub is mounted on. The handshake cannot
    /// send an Authorization header, so the token arrives in the query string for
    /// this path only.
    /// </summary>
    private const string HubPath = "/hubs";

    public static void AddAuthuntication(IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "The Jwt configuration section is missing. Set Jwt:SigningKey, Jwt:Issuer and Jwt:Audience "
                + "through user-secrets in development or the environment in every other environment.");

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            // Refusing to start beats starting with a guessable key: a weak signing key
            // lets anyone who guesses it mint a token for any user, including an admin.
            throw new InvalidOperationException(
                "Jwt:SigningKey must be set to at least 32 characters. It must never be committed to source control.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.SaveToken = true;

            // Metadata is only fetched for OIDC discovery, which this scheme does not
            // use - but leaving it off would also permit plaintext discovery once an
            // OIDC scheme is added, so it tracks the environment instead.
            options.RequireHttpsMetadata = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = signingKey,

                // No skew. The default five minutes means a revoked or expired token
                // keeps working for five more minutes, which is the whole point of a
                // short access-token lifetime undone.
                ClockSkew = TimeSpan.Zero,
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // WebSocket handshakes cannot set headers, so SignalR passes the
                    // token as a query parameter. Accepted only for the hub path, so a
                    // token can never end up in the access log of a normal endpoint.
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(HubPath))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },

                OnAuthenticationFailed = context =>
                {
                    // Lets the SPA tell "expired, refresh and retry" apart from
                    // "invalid, sign out" without parsing the token itself.
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers["IS-TOKEN-EXPIRED"] = "true";
                    }

                    return Task.CompletedTask;
                },
            };
        });
    }
}
