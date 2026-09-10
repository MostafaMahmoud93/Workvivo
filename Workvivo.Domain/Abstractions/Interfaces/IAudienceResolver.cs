namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Works out which audience keys reach a given employee.
///
/// The whole targeting design turns on this. Content carries rows saying who it is for
/// (<c>ALL</c>, <c>DEPT:{id}</c>, <c>COMM:{id}</c>…); this produces the matching set for
/// one viewer, and the feed query is then a single indexed <c>IN</c> against it rather
/// than a union of one predicate per targeting dimension.
/// </summary>
public interface IAudienceResolver
{
    /// <summary>
    /// Every audience key that reaches this employee - typically 10 to 30 short strings
    /// even in a large organisation.
    /// </summary>
    Task<IReadOnlyList<string>> ResolveKeysAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>Drops the cached key set, after a transfer, a role change or a community join.</summary>
    Task InvalidateAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
