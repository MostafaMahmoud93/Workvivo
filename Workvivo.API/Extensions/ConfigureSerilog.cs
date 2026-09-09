using Serilog;
using Serilog.Events;

namespace Workvivo.API.Extensions;

public static class ConfigureSerilog
{
    /// <summary>
    /// Structured logging, configured from appsettings so a deployment can change
    /// levels and sinks without a rebuild. The code below only supplies defaults for
    /// what configuration does not say.
    /// </summary>
    public static void AddWorkvivoSerilog(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, config) => config
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "Workvivo.API")
            // EF Core logs every command at Information. On a feed request that is
            // dozens of lines of SQL per page view, which drowns everything else and
            // can put parameter values - names, emails - into the log.
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Hangfire", LogEventLevel.Warning));
    }

    /// <summary>
    /// One summary line per HTTP request, replacing the several ASP.NET Core emits by
    /// default, enriched with the route, the user and the correlation id.
    /// </summary>
    public static IApplicationBuilder UseWorkvivoRequestLogging(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex is not null || httpContext.Response.StatusCode >= 500)
                {
                    return LogEventLevel.Error;
                }

                // Health probes run every few seconds forever. At Information they are
                // most of the log volume and none of its value.
                if (httpContext.Request.Path.StartsWithSegments("/health"))
                {
                    return LogEventLevel.Verbose;
                }

                return httpContext.Response.StatusCode >= 400 || elapsed > 2000
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserId", httpContext.User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            };
        });

        return app;
    }
}
