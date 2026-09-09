using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Workvivo.API.Extensions;

public static class ConfigureRateLimiting
{
    /// <summary>Applied to the sign-in and password endpoints.</summary>
    public const string AuthPolicy = "auth";

    /// <summary>Applied to upload endpoints.</summary>
    public const string UploadPolicy = "upload";

    /// <summary>
    /// Uses the framework's own rate limiter rather than a package.
    ///
    /// Two tiers: a wide global limit that stops one client saturating the API, and a
    /// deliberately tight limit on authentication, where the thing being protected is
    /// not capacity but passwords. Account lockout already covers a single account;
    /// this covers spraying one password across many accounts, which lockout misses
    /// entirely.
    /// </summary>
    public static IServiceCollection AddWorkvivoRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsync(
                    """
                    {"type":"https://workvivo/errors/rate-limit","title":"Too many requests.","status":429,"success":false}
                    """,
                    cancellationToken);
            };

            // Partitioned by authenticated user where possible, falling back to remote
            // address. Keying purely on IP would make one office's NAT gateway share a
            // single bucket across hundreds of staff.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(UploadPolicy, context =>
                RateLimitPartition.GetConcurrencyLimiter(
                    PartitionKey(context),
                    _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = 3,
                        QueueLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    }));
        });

        return services;
    }

    private static string PartitionKey(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true
            ? "user:" + context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
}
