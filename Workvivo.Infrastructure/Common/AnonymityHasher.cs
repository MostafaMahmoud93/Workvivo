using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Infrastructure.Common;

/// <summary>
/// HMAC-SHA256 over the scope and the employee, keyed by a server secret.
///
/// The key never leaves configuration and is never written next to the hashes it
/// produces, which is the property that makes an anonymous response actually
/// anonymous if the database is copied.
/// </summary>
public sealed class AnonymityHasher : IAnonymityHasher
{
    /// <summary>Where the secret comes from. Supplied as <c>Anonymity__Key</c> outside development.</summary>
    public const string KeyPath = "Anonymity:Key";

    private readonly byte[] _key;

    public AnonymityHasher(IConfiguration configuration)
    {
        var configured = configuration[KeyPath];

        if (string.IsNullOrWhiteSpace(configured) || configured.Length < 32)
        {
            // Refusing to start beats starting with a guessable key. A short or absent
            // key makes every anonymous response reversible by anybody who can list
            // the employees, and nothing about the running system would look wrong.
            throw new InvalidOperationException(
                $"{KeyPath} must be set to at least 32 characters. Anonymous polls and surveys "
                + "depend on it, and it must never be committed to source control.");
        }

        _key = Encoding.UTF8.GetBytes(configured);
    }

    public string Hash(Guid scopeId, Guid employeeId)
    {
        // The scope is part of the message, so the same person hashes differently in
        // every poll. Without it, one leaked mapping would deanonymise them everywhere.
        var message = Encoding.UTF8.GetBytes($"{scopeId:N}:{employeeId:N}");

        return Convert.ToHexString(HMACSHA256.HashData(_key, message));
    }
}
