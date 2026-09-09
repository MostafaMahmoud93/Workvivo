using Microsoft.AspNetCore.Http;
using Shouldly;
using Workvivo.API.Middleware;
using Xunit;

namespace Workvivo.API.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    [InlineData("Cross-Origin-Opener-Policy", "same-origin")]
    public async Task Sets_the_expected_security_header(string header, string expected)
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[header].ToString().ShouldBe(expected);
    }

    [Fact]
    public async Task Denies_framing_and_active_content_for_API_responses()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var csp = context.Response.Headers.ContentSecurityPolicy.ToString();
        csp.ShouldContain("default-src 'none'");
        csp.ShouldContain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task Calls_the_next_middleware()
    {
        var called = false;
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        called.ShouldBeTrue();
    }
}
