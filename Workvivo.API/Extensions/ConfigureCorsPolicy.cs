using Workvivo.API.Options;

namespace Workvivo.API.Extensions;

public static class ConfigureCorsPolicy
{
    /// <summary>
    /// One named policy, built from configured origins.
    ///
    /// AllowCredentials is required because the refresh token travels in a cookie, and
    /// the CORS spec forbids combining that with a wildcard origin - so the allow-list
    /// is not optional, it is the only thing that can work.
    /// </summary>
    public static IServiceCollection AddWorkvivoCors(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

        services.AddCors(cors =>
        {
            cors.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                if (options.AllowedOrigins.Length == 0)
                {
                    // No origins configured: allow nothing rather than everything. A
                    // same-origin deployment (SPA behind the same host) needs no CORS
                    // at all, so this is the correct default there too.
                    policy.WithOrigins([]);
                    return;
                }

                policy.WithOrigins(options.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    // Lets the SPA read the correlation id off a failed response so it
                    // can show it in an error toast.
                    .WithExposedHeaders("X-Correlation-Id", "IS-TOKEN-EXPIRED");
            });
        });

        return services;
    }
}
