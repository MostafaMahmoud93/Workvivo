using System.Linq.Expressions;
using Hangfire;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Infrastructure.Jobs;

/// <summary>
/// Hangfire-backed implementation of <see cref="IBackgroundJobScheduler"/>.
///
/// Hangfire serialises the expression, so the arguments must be simple values the
/// worker can rehydrate - ids, not entities. Passing a tracked entity would capture a
/// DbContext that is long gone by the time the job runs.
/// </summary>
public sealed class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly IBackgroundJobClient _client;
    private readonly IRecurringJobManager _recurring;

    public HangfireBackgroundJobScheduler(IBackgroundJobClient client, IRecurringJobManager recurring)
    {
        _client = client;
        _recurring = recurring;
    }

    public string Enqueue<TService>(Expression<Func<TService, Task>> methodCall) where TService : notnull =>
        _client.Enqueue(methodCall);

    public string Schedule<TService>(Expression<Func<TService, Task>> methodCall, TimeSpan delay) where TService : notnull =>
        _client.Schedule(methodCall, delay);

    public void AddOrUpdateRecurring<TService>(
        string recurringJobId,
        Expression<Func<TService, Task>> methodCall,
        string cronExpression) where TService : notnull =>
        _recurring.AddOrUpdate(recurringJobId, methodCall, cronExpression);

    public bool Delete(string jobId) => _client.Delete(jobId);
}
