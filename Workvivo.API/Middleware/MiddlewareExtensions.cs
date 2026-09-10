namespace Workvivo.API.Middleware;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Order matters and is not arbitrary:
    ///
    ///   correlation id   outermost, so every later log line and the error body carry it
    ///   security headers next, so they are applied to error responses too
    ///   exceptions       innermost, so it converts the exception before anything
    ///                    outside it observes the status code
    ///
    /// Request logging is added *between* the headers and the exception handler by
    /// UseWorkvivoRequestLogging, for a reason worth stating: nested inside the
    /// exception handler it sees the raw exception and records a handled 409 or 404 as
    /// a 500, which turns the error log into noise and hides real failures.
    /// </summary>
    public static IApplicationBuilder UseWorkvivoDiagnostics(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }

    /// <summary>
    /// Converts exceptions into ProblemDetails. Registered after request logging so the
    /// logged status is the one the client actually received.
    /// </summary>
    public static IApplicationBuilder UseWorkvivoExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        return app;
    }
}
