using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Exceptions;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Application.Behaviors;

/// <summary>
/// One structured log line per use case, with the caller, the correlation id and how
/// long it took.
///
/// Expected failures (anything deriving from AppException) are logged at Warning with
/// no stack trace - a 404 is not an incident and filling the error log with them
/// hides the ones that are. Everything else is logged at Error with the full trace.
/// </summary>
public sealed class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUser _currentUser;

    public RequestLoggingBehavior(
        ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUser currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        // The request object itself is never logged: commands carry passwords, post
        // bodies and personal data. Only its type name goes to the log.
        _logger.LogInformation(
            "Handling {RequestName} for user {UserId} (correlation {CorrelationId})",
            requestName,
            _currentUser.UserId,
            _currentUser.CorrelationId);

        try
        {
            var response = await next();

            stopwatch.Stop();
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (AppException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "{RequestName} rejected after {ElapsedMilliseconds} ms: {ErrorType} - {Message}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                ex.ErrorType,
                ex.Message);
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "{RequestName} cancelled after {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "{RequestName} failed after {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
