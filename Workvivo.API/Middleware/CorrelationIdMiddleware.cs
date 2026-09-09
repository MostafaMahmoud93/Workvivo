using Serilog.Context;
using Workvivo.Infrastructure.Common;

namespace Workvivo.API.Middleware;

/// <summary>
/// Gives every request an id, pushes it onto the Serilog context and echoes it back
/// in a response header.
///
/// This is what turns "it failed around 3pm" into a single log query. The id is also
/// returned inside every ProblemDetails, so a screenshot of an error is enough to
/// find the exact request that produced it.
///
/// An inbound id is honoured so a call chain across services keeps one trace - but it
/// is length-limited and stripped of control characters first, because it goes
/// straight into log output and an unbounded caller-controlled string in a log is how
/// log injection works.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const int MaxLength = 64;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Sanitize(context.Request.Headers[CurrentUser.CorrelationIdHeader].FirstOrDefault())
            ?? context.TraceIdentifier;

        context.Items[CurrentUser.CorrelationIdHeader] = correlationId;

        // Set on OnStarting rather than now: writing a header after the response has
        // begun throws, and something downstream may start streaming.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CurrentUser.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return null;
        }

        foreach (var c in value)
        {
            // Letters, digits, dash and underscore only - no newlines to forge log
            // entries with, no separators to confuse a parser.
            if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')
            {
                return null;
            }
        }

        return value;
    }
}
