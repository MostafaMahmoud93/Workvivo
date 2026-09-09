using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Application.Behaviors;

/// <summary>
/// Flags use cases that run long enough to matter.
///
/// The threshold is a warning, not a failure: the goal is that a query which starts
/// degrading as the post table grows shows up in the logs before it shows up in a
/// support ticket.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarnThresholdMilliseconds = 500;

    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUser _currentUser;

    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
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
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > WarnThresholdMilliseconds)
        {
            _logger.LogWarning(
                "Slow request: {RequestName} took {ElapsedMilliseconds} ms for user {UserId} (correlation {CorrelationId})",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds,
                _currentUser.UserId,
                _currentUser.CorrelationId);
        }

        return response;
    }
}
