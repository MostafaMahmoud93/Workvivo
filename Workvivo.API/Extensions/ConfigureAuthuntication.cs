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

    /// <summary>
    /// Reports what configuration actually contains, rather than what it ought to.
    ///
    /// An earlier version of this inferred the cause from the environment name -
    /// "you are in Development, so user secrets are loaded". That inference is
    /// precisely what fails when the message is needed: the environment can be
    /// Development and the secrets file still not reach the configuration root,
    /// and a message asserting otherwise sends the reader to check the one thing
    /// that is already correct.
    ///
    /// So it asks. The provider list comes from the configuration root, and each
    /// provider is queried for the key by name. No value is ever printed - only
    /// whether a provider holds one - because this text ends up in logs.
    /// </summary>
    private static string Remedy(IConfiguration configuration, IHostEnvironment environment)
    {
        var report = new StringBuilder();

        report.Append($"Running as '{environment.EnvironmentName}'. ");

        if (configuration is not IConfigurationRoot root)
        {
            return report
                .Append("Configuration is not a root, so its providers cannot be listed.")
                .ToString();
        }

        var providers = root.Providers.ToList();

        var secrets = providers.Find(provider =>
            provider.GetType().Name.Contains("Json", StringComparison.Ordinal)
            && provider.ToString()?.Contains("secrets.json", StringComparison.OrdinalIgnoreCase) == true);

        report.Append(secrets is null
            ? "The user-secrets provider is NOT loaded - only Development loads it, and only when "
                + "the entry assembly carries a UserSecretsId. "
            : "The user-secrets provider IS loaded. ");

        // Which provider, if any, actually holds the key. The last one to answer
        // wins in configuration, so naming them all shows an override too.
        var holders = providers
            .Where(provider => provider.TryGet($"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}", out var value)
                && !string.IsNullOrWhiteSpace(value))
            .Select(provider => provider.ToString() ?? provider.GetType().Name)
            .ToList();

        report.Append(holders.Count == 0
            ? "No configuration provider supplies Jwt:SigningKey. "
            : $"Supplied by: {string.Join(" then ", holders)} (the last one wins). ");

        report.Append(environment.IsDevelopment()
            ? "Set it with: dotnet user-secrets set \"Jwt:SigningKey\" \"<64+ random characters>\" "
                + "--project Workvivo.API"
            : "Supply Jwt__SigningKey from the environment or a secret store, or set "
                + "ASPNETCORE_ENVIRONMENT=Development if this was meant to be a local run.");

        return report.ToString();
    }

    public static void AddAuthuntication(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "The Jwt configuration section is missing. " + Remedy(configuration, environment));

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            // Refusing to start beats starting with a guessable key: a weak signing key
            // lets anyone who guesses it mint a token for any user, including an admin.
            //
            // The message names the environment because that is the whole diagnosis.
            // The key being absent means two completely different things depending on
            // it, and they need opposite fixes - so a message that says only "it is
            // missing" sends people to check the one place that is already correct.
            throw new InvalidOperationException(
                "Jwt:SigningKey must be set to at least 32 characters. It must never be "
                + "committed to source control. " + Remedy(configuration, environment));
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
