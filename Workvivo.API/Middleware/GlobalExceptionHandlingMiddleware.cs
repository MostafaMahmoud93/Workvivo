using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Common;

namespace Workvivo.API.Middleware;

/// <summary>
/// Turns every unhandled exception into an RFC 7807 ProblemDetails response.
///
/// One place decides what a client sees when something fails, which is the only way
/// to be sure no endpoint leaks a stack trace, a connection string or an inner
/// exception message. Deliberate failures (AppException) carry their own status and
/// keep their message; anything else becomes a flat 500 whose body says nothing,
/// while the full detail goes to the log under the same correlation id.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware
{
    private const string ProblemTypeBase = "https://workvivo/errors/";

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client hung up. Not an error, and there is nobody left to answer.
            _logger.LogInformation("Request {Path} was cancelled by the client", context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items.TryGetValue(CurrentUser.CorrelationIdHeader, out var value)
            ? value as string
            : context.TraceIdentifier;

        if (context.Response.HasStarted)
        {
            // Too late to change the status or the body; all that is left is a log
            // entry an operator can find.
            _logger.LogError(
                exception,
                "Unhandled exception after the response started for {Method} {Path} (correlation {CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                correlationId);
            return;
        }

        var problem = BuildProblem(exception, correlationId);

        if (exception is AppException appException)
        {
            _logger.LogWarning(
                "{ErrorType} on {Method} {Path}: {Message} (correlation {CorrelationId})",
                appException.ErrorType,
                context.Request.Method,
                context.Request.Path,
                appException.Message,
                correlationId);
        }
        else
        {
            _logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path} (correlation {CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                correlationId);
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, JsonSerializerOptions.Web),
            context.RequestAborted);
    }

    private ProblemDetails BuildProblem(Exception exception, string? correlationId)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case ValidationException validation:
                problem = new ValidationProblemDetails(
                    validation.Errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
                {
                    Title = validation.Message,
                    Status = validation.StatusCode,
                    Type = ProblemTypeBase + validation.ErrorType,
                };
                break;

            case AppException app:
                problem = new ProblemDetails
                {
                    Title = app.Message,
                    Status = app.StatusCode,
                    Type = ProblemTypeBase + app.ErrorType,
                };

                if (app.Extensions is not null)
                {
                    foreach (var (key, value) in app.Extensions)
                    {
                        problem.Extensions[key] = value;
                    }
                }

                break;

            default:
                problem = new ProblemDetails
                {
                    // Nothing from the exception reaches the client here. The message
                    // could name a table, a file path or a host.
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Type = ProblemTypeBase + "server-error",
                };

                if (_environment.IsDevelopment())
                {
                    problem.Detail = exception.ToString();
                }

                break;
        }

        problem.Extensions["traceId"] = correlationId;

        // Mirrors the ApiResponse envelope, so a client can branch on `success`
        // without caring whether the payload was a success body or a problem body.
        problem.Extensions["success"] = false;

        return problem;
    }
}
