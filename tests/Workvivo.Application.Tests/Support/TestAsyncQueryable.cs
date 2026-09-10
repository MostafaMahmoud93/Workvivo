using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Workvivo.Application.Tests.Support;

/// <summary>
/// An in-memory queryable that survives <c>ToListAsync</c>, <c>FirstOrDefaultAsync</c>
/// and the rest of EF's async operators.
///
/// A plain <c>List&lt;T&gt;.AsQueryable()</c> does not: those operators require the
/// provider to implement <see cref="IAsyncQueryProvider"/>, and a handler under test
/// throws "the source IQueryable does not implement IAsyncEnumerable" the moment it
/// awaits anything.
///
/// This exists so notification handlers can be tested against substituted repositories
/// - real handler logic, no database. The alternative, spinning up EF's in-memory
/// provider, needs the whole DbContext and its SQL-Server-specific configuration,
/// which is a heavier and more fragile dependency for testing a branch in a handler.
/// </summary>
internal sealed class TestAsyncQueryable<T> : IAsyncEnumerable<T>, IOrderedQueryable<T>, IQueryProvider, IAsyncQueryProvider
{
    private readonly IQueryable<T> _inner;

    public TestAsyncQueryable(IEnumerable<T> items)
    {
        _inner = items.AsQueryable();
        Expression = _inner.Expression;
    }

    private TestAsyncQueryable(Expression expression, IQueryable<T> inner)
    {
        Expression = expression;
        _inner = inner;
    }

    public Type ElementType => typeof(T);

    public Expression Expression { get; }

    public IQueryProvider Provider => this;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        foreach (var item in _inner)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return item;
        }

        await Task.CompletedTask;
    }

    public IQueryable CreateQuery(Expression expression) => CreateQuery<T>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncQueryable<TElement>(expression, _inner.Provider.CreateQuery<TElement>(expression));

    public object? Execute(Expression expression) => _inner.Provider.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Provider.Execute<TResult>(expression);

    /// <summary>
    /// EF routes every async operator through here. Scalar operators ask for
    /// <c>Task&lt;TResult&gt;</c>; streaming ones ask for an async sequence, so both
    /// shapes have to be produced.
    /// </summary>
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult);

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var itemType = resultType.GetGenericArguments()[0];

            var value = typeof(IQueryProvider)
                .GetMethods()
                .First(method => method.Name == nameof(IQueryProvider.Execute) && method.IsGenericMethod)
                .MakeGenericMethod(itemType)
                .Invoke(_inner.Provider, [expression]);

            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(itemType)
                .Invoke(null, [value])!;
        }

        return (TResult)(object)new TestAsyncQueryable<T>(expression, _inner.Provider.CreateQuery<T>(expression));
    }
}

internal static class TestQueryable
{
    public static IQueryable<T> AsTestQueryable<T>(this IEnumerable<T> items) => new TestAsyncQueryable<T>(items);
}
