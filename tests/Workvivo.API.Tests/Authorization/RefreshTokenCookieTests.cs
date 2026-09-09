using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Workvivo.API.Authorization;
using Workvivo.API.Tests.Support;
using Xunit;

namespace Workvivo.API.Tests.Authorization;

/// <summary>
/// The refresh cookie carries a fourteen-day credential. Its attributes are the only
/// thing standing between that and a cross-site request or a script on the page, so
/// each one is pinned here rather than left to be noticed if it regresses.
/// </summary>
public class RefreshTokenCookieTests
{
    private static IHostEnvironment Environment(string name)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(name);
        return environment;
    }

    private static string WriteAndCapture(string environmentName)
    {
        var response = new StartableResponseFeature();
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        features.Set<IHttpResponseFeature>(response);

        var context = new DefaultHttpContext(features);

        RefreshTokenCookie.Write(
            context, Environment(environmentName), "the-token", DateTime.UtcNow.AddDays(14));

        return context.Response.Headers.SetCookie.ToString();
    }

    [Fact]
    public void The_production_cookie_is_HttpOnly_Secure_and_SameSite_Strict()
    {
        var header = WriteAndCapture(Environments.Production);

        // HttpOnly is the one that matters most: it is what stops an XSS payload
        // reading a fourteen-day credential out of the page.
        header.ShouldContain("httponly", Case.Insensitive);
        header.ShouldContain("secure", Case.Insensitive);
        header.ShouldContain("samesite=strict", Case.Insensitive);
    }

    [Fact]
    public void The_production_cookie_uses_the_host_prefix()
    {
        // __Host- is only accepted by a browser on a Secure cookie with no Domain, so a
        // compromised sibling subdomain cannot set one the API would then trust.
        WriteAndCapture(Environments.Production).ShouldContain(RefreshTokenCookie.SecureName);
    }

    [Fact]
    public void The_cookie_is_scoped_to_the_auth_endpoints()
    {
        // Path scoping keeps the long-lived credential off every feed request and file
        // upload - it is only ever sent where it is actually needed.
        WriteAndCapture(Environments.Production).ShouldContain("path=/api/auth", Case.Insensitive);
    }

    [Fact]
    public void Development_drops_the_host_prefix_because_it_requires_Secure()
    {
        // Browsers reject a __Host- cookie that is not Secure, and development runs
        // over plain HTTP - so keeping the prefix would silently break sign-in locally.
        var header = WriteAndCapture(Environments.Development);

        header.ShouldContain(RefreshTokenCookie.DevelopmentName);
        header.ShouldNotContain("__Host-");
    }

    [Fact]
    public void The_cookie_path_is_lowercase_to_match_the_routes()
    {
        // Cookie path matching is case-sensitive (RFC 6265). If the routes were
        // /api/Auth/... the browser would not send this cookie and refresh would fail
        // with a 401 that looks exactly like an expired session.
        WriteAndCapture(Environments.Production).ShouldContain("path=/api/auth");
        Workvivo.Application.Bases.RouteClass.Auth.Refresh.ShouldBe("api/auth/refresh");
        Workvivo.Application.Bases.RouteClass.Auth.Login.ShouldBe("api/auth/login");
    }
}
