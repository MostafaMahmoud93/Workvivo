using System.Linq.Expressions;

namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Queues work that must not happen inside a request.
///
/// An abstraction over Hangfire rather than a direct dependency, so application code
/// does not take a reference to a job framework and tests can assert "this handler
/// enqueued a notification fan-out" without a running server.
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>Runs as soon as a worker is free.</summary>
    string Enqueue<TService>(Expression<Func<TService, Task>> methodCall) where TService : notnull;

    /// <summary>Runs once, after a delay.</summary>
    string Schedule<TService>(Expression<Func<TService, Task>> methodCall, TimeSpan delay) where TService : notnull;

    /// <summary>Registers or updates a recurring job identified by <paramref name="recurringJobId"/>.</summary>
    void AddOrUpdateRecurring<TService>(string recurringJobId, Expression<Func<TService, Task>> methodCall, string cronExpression)
        where TService : notnull;

    bool Delete(string jobId);
}
