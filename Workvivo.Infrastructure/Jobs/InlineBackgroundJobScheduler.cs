using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Infrastructure.Jobs;

/// <summary>
/// Fallback used when Hangfire is switched off - tests, and any environment without a
/// job database.
///
/// Runs the work immediately on a background task in this process. Deliberately not
/// silent: unlike Hangfire there is no persistence, no retry and no visibility, so a
/// process restart loses queued work. Every call is logged at Warning so it is obvious
/// when a deployment is running in this mode by accident.
/// </summary>
public sealed class InlineBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InlineBackgroundJobScheduler> _logger;

    public InlineBackgroundJobScheduler(IServiceScopeFactory scopeFactory, ILogger<InlineBackgroundJobScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public string Enqueue<TService>(Expression<Func<TService, Task>> methodCall) where TService : notnull
    {
        var jobId = Guid.NewGuid().ToString("N");
        _logger.LogWarning(
            "Background jobs are disabled: running {Service} inline as {JobId} with no retry or persistence",
            typeof(TService).Name,
            jobId);

        _ = Task.Run(() => InvokeAsync(methodCall, jobId));
        return jobId;
    }

    public string Schedule<TService>(Expression<Func<TService, Task>> methodCall, TimeSpan delay) where TService : notnull
    {
        var jobId = Guid.NewGuid().ToString("N");
        _logger.LogWarning(
            "Background jobs are disabled: running {Service} inline after {Delay} as {JobId}",
            typeof(TService).Name,
            delay,
            jobId);

        _ = Task.Run(async () =>
        {
            await Task.Delay(delay);
            await InvokeAsync(methodCall, jobId);
        });

        return jobId;
    }

    public void AddOrUpdateRecurring<TService>(
        string recurringJobId,
        Expression<Func<TService, Task>> methodCall,
        string cronExpression) where TService : notnull =>
        _logger.LogWarning(
            "Background jobs are disabled: recurring job {RecurringJobId} ({Cron}) will not run",
            recurringJobId,
            cronExpression);

    public bool Delete(string jobId) => false;

    private async Task InvokeAsync<TService>(Expression<Func<TService, Task>> methodCall, string jobId)
        where TService : notnull
    {
        try
        {
            // A fresh scope, because the request scope that queued the work is gone.
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            await methodCall.Compile()(service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inline job {JobId} for {Service} failed", jobId, typeof(TService).Name);
        }
    }
}
