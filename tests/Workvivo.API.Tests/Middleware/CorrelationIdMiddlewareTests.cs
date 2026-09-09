using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Shouldly;
using Workvivo.API.Middleware;
using Workvivo.API.Tests.Support;
using Workvivo.Infrastructure.Common;
using Xunit;

namespace Workvivo.API.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private static async Task<HttpContext> RunAsync(string? inboundHeader)
    {
        var response = new StartableResponseFeature();
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        features.Set<IHttpResponseFeature>(response);

        var context = new DefaultHttpContext(features) { TraceIdentifier = "trace-fallback" };

        if (inboundHeader is not null)
        {
            context.Request.Headers[CurrentUser.CorrelationIdHeader] = inboundHeader;
        }

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        // The middleware defers the header to OnStarting, so nothing is written until
        // the response begins.
        await response.FireOnStartingAsync();

        return context;
    }

    [Fact]
    public async Task Honours_a_well_formed_inbound_correlation_id()
    {
        var context = await RunAsync("abc-123_XYZ");

        context.Items[CurrentUser.CorrelationIdHeader].ShouldBe("abc-123_XYZ");
        context.Response.Headers[CurrentUser.CorrelationIdHeader].ToString().ShouldBe("abc-123_XYZ");
    }

    [Fact]
    public async Task Generates_one_when_the_caller_sends_none()
    {
        var context = await RunAsync(null);

        context.Items[CurrentUser.CorrelationIdHeader].ShouldBe("trace-fallback");
        context.Response.Headers[CurrentUser.CorrelationIdHeader].ToString().ShouldBe("trace-fallback");
    }

    [Theory]
    [InlineData("bad id with spaces")]
    [InlineData("inject\r\nFATAL Everything is fine")]
    [InlineData("semi;colon")]
    [InlineData("../../etc/passwd")]
    public async Task Rejects_a_correlation_id_that_could_forge_a_log_entry(string hostile)
    {
        // The value goes into log output verbatim. A newline in it would let a caller
        // write whatever they like into the log as though the server had said it.
        var context = await RunAsync(hostile);

        context.Items[CurrentUser.CorrelationIdHeader].ShouldBe("trace-fallback");
    }

    [Fact]
    public async Task Rejects_an_over_long_correlation_id()
    {
        var context = await RunAsync(new string('a', 65));

        context.Items[CurrentUser.CorrelationIdHeader].ShouldBe("trace-fallback");
    }

    [Fact]
    public async Task Accepts_a_correlation_id_at_the_length_limit()
    {
        var atLimit = new string('a', 64);

        var context = await RunAsync(atLimit);

        context.Items[CurrentUser.CorrelationIdHeader].ShouldBe(atLimit);
    }
}
