using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Infrastructure.Common;

/// <summary>
/// Reads the caller out of the current HTTP context.
///
/// The one place in the system allowed to touch IHttpContextAccessor. Everything else
/// takes ICurrentUser, which means handlers stay testable and a background job can be
/// given a different implementation rather than pretending to have a request.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    /// <summary>Header name matching CorrelationIdMiddleware.</summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName =>
        Principal?.FindFirstValue(ClaimTypes.Name) ?? Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsAdmin =>
        bool.TryParse(Principal?.FindFirstValue("IsAdmin"), out var isAdmin) && isAdmin;

    public string? UserTypeCode => Principal?.FindFirstValue("UserTypeCode");

    /// <summary>
    /// The connection's remote address. X-Forwarded-For is deliberately not read here:
    /// a client can send that header itself. Behind a proxy, configure
    /// ForwardedHeadersOptions with the known proxy addresses so the framework
    /// rewrites RemoteIpAddress from a header it has decided to trust.
    /// </summary>
    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua
            ? Truncate(ua, 512)
            : null;

    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Items.TryGetValue(CorrelationIdHeader, out var value) == true
            ? value as string
            : _httpContextAccessor.HttpContext?.TraceIdentifier;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
