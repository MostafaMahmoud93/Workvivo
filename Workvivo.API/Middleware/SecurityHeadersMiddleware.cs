namespace Workvivo.API.Middleware;

/// <summary>
/// Adds the response headers that tell a browser to be strict.
///
/// Cheap, and each one closes a real class of attack: MIME sniffing turning an upload
/// into script, the API being framed for clickjacking, internal URLs leaking through
/// the Referer header on outbound links.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Never let the browser second-guess a Content-Type. Without this, an uploaded
        // file served as application/octet-stream can be sniffed as HTML and run.
        headers["X-Content-Type-Options"] = "nosniff";

        // This host serves JSON and, in development, Swagger. Nothing here should ever
        // be framed.
        headers["X-Frame-Options"] = "DENY";

        // Send the origin but not the path to third parties, so internal URLs
        // containing ids do not leak through Referer.
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-site";

        // A CSP for the API itself. The Angular app is served separately and carries
        // its own, richer policy - this one just makes sure an API response can never
        // be rendered as an active document.
        headers["Content-Security-Policy"] =
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

        // The "Server: Kestrel" advertisement is suppressed at the server level in
        // Program.cs - Kestrel writes that header after middleware has run, so removing
        // it here would silently do nothing.

        return _next(context);
    }
}
