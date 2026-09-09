namespace Workvivo.API.Middleware;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Order matters and is not arbitrary:
    ///
    ///   correlation id  first, so every later log line and the error body carry it
    ///   exceptions      next, so it wraps everything downstream including routing
    ///   security headers last, so they are applied to error responses too
    /// </summary>
    public static IApplicationBuilder UseWorkvivoDiagnostics(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
