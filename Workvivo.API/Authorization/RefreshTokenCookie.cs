namespace Workvivo.API.Authorization;

/// <summary>
/// Reads and writes the refresh-token cookie.
///
/// The refresh token lives in a cookie rather than in the response body for one
/// reason: a single cross-site scripting flaw anywhere in the SPA can read
/// localStorage and walk away with a fourteen-day credential. HttpOnly puts it out of
/// JavaScript's reach entirely, so the same flaw yields only the in-memory access
/// token, which expires in fifteen minutes.
///
/// The cost is CSRF exposure, since cookies are sent automatically. That is closed
/// three ways: SameSite=Strict, a Path that scopes the cookie to the auth endpoints
/// alone, and the __Host- prefix, which browsers only accept on a Secure cookie with
/// no Domain attribute - so a subdomain cannot set one that the API would then trust.
/// </summary>
public static class RefreshTokenCookie
{
    /// <summary>
    /// The __Host- prefix is dropped outside production because browsers require
    /// Secure for it, and development runs over plain HTTP on localhost.
    /// </summary>
    public const string SecureName = "__Host-wv_rt";
    public const string DevelopmentName = "wv_rt";

    /// <summary>
    /// Scoped to the auth endpoints. The cookie is never attached to a feed request or
    /// a file upload, so the long-lived credential is not sprayed across every call.
    /// </summary>
    private const string Path = "/api/auth";

    public static string NameFor(IHostEnvironment environment) =>
        environment.IsDevelopment() ? DevelopmentName : SecureName;

    public static string? Read(HttpContext context, IHostEnvironment environment) =>
        context.Request.Cookies[NameFor(environment)];

    public static void Write(
        HttpContext context,
        IHostEnvironment environment,
        string token,
        DateTime expiresAtUtc)
    {
        context.Response.Cookies.Append(NameFor(environment), token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = Path,
            Expires = new DateTimeOffset(expiresAtUtc, TimeSpan.Zero),
            IsEssential = true,
        });
    }

    public static void Clear(HttpContext context, IHostEnvironment environment)
    {
        // Deleting a cookie only works when the attributes match the ones it was set
        // with, so Path and Secure are repeated here rather than left to defaults.
        context.Response.Cookies.Delete(NameFor(environment), new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = Path,
        });
    }
}
