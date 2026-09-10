namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Produces the per-instrument voter hash that makes an anonymous poll or survey
/// enforceable without recording who answered.
///
/// The problem: an anonymous poll still has to stop one person voting a hundred
/// times, which needs a per-voter unique value - but storing the employee id defeats
/// the anonymity entirely.
///
/// The answer is a keyed hash, scoped to one instrument. It gives uniqueness within
/// that poll and nothing else:
///
/// - it cannot be matched against another poll's hashes, because the scope is mixed in;
/// - it cannot be reversed by hashing every employee id, because the key is secret;
/// - it survives a database leak, because the key is not in the database.
///
/// A plain SHA-256 of the employee id would fail the second point outright: an
/// attacker with the table and a list of employees recovers every voter in seconds.
/// </summary>
public interface IAnonymityHasher
{
    /// <summary>
    /// A stable, opaque token for this person within this instrument.
    /// </summary>
    /// <param name="scopeId">The poll or survey the hash is confined to.</param>
    string Hash(Guid scopeId, Guid employeeId);
}
