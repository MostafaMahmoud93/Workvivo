using System.Security.Cryptography;
using System.Text;

namespace Workvivo.Infrastructure.Abstractions;

/// <summary>
/// Derives a stable GUID from a string.
///
/// Seed data has to be deterministic: EF compares the model it builds against the
/// migration snapshot, and a <c>Guid.NewGuid()</c> anywhere in a <c>HasData</c> call
/// makes the model differ on every build - which is exactly the failure that stopped
/// "ef database update" running earlier in this project.
///
/// The alternative is a hundred hand-written GUID literals nobody can read. Deriving
/// them from meaningful names - <c>perm:Post.Create</c> - keeps the seed legible and
/// stable, and makes cross-references between seeded rows obvious rather than a
/// matching exercise between two opaque literals.
///
/// Not a security primitive. MD5 is used because it produces exactly the 16 bytes a
/// GUID needs and only has to be stable, not collision-resistant against an attacker.
/// </summary>
public static class DeterministicGuid
{
    public static Guid From(string name) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes(name)));
}
