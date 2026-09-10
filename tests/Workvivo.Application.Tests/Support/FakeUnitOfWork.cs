using NSubstitute;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.BaseEntities;

namespace Workvivo.Application.Tests.Support;

/// <summary>
/// A unit of work whose repositories are backed by lists.
///
/// Handlers reach for <c>Repository&lt;T, TKey&gt;()</c>, so faking that one generic
/// method is enough to exercise real handler logic - the branching, the ordering, the
/// filtering - without a database. What it deliberately does not model is anything the
/// database enforces: unique indexes, check constraints, <c>ExecuteUpdate</c>. Rules
/// that live there need an integration test, and pretending otherwise here would be a
/// test that passes while production fails.
/// </summary>
internal sealed class FakeUnitOfWork
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public IUnitOfWork Object => _unitOfWork;

    /// <summary>Everything handed to <c>AddAsync</c>, in order, per entity type.</summary>
    public Dictionary<Type, List<object>> Added { get; } = [];

    public int SaveCount { get; private set; }

    public FakeUnitOfWork()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                SaveCount++;
                return Task.FromResult(1);
            });
    }

    public FakeUnitOfWork With<T, TKey>(IEnumerable<T> rows) where T : BaseCommonEntity<TKey>
    {
        var repository = Substitute.For<IBaseRepository<T, TKey>>();
        var stored = rows.ToList();

        repository.GetAllQ().Returns(_ => stored.AsTestQueryable());

        repository.AddAsync(Arg.Any<T>()).Returns(call =>
        {
            var entity = call.Arg<T>();

            if (!Added.TryGetValue(typeof(T), out var list))
            {
                list = [];
                Added[typeof(T)] = list;
            }

            list.Add(entity);
            stored.Add(entity);

            return Task.FromResult(entity);
        });

        _unitOfWork.Repository<T, TKey>().Returns(repository);

        return this;
    }

    public IReadOnlyList<T> AddedOf<T>() =>
        Added.TryGetValue(typeof(T), out var list) ? [.. list.Cast<T>()] : [];
}
