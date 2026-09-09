using Workvivo.Domain.Abstractions.Enums;

namespace Workvivo.Domain.Models.Storage;

/// <summary>
/// The outcome of storing a file: enough to write the metadata row and to fetch the
/// bytes back later. The bytes themselves never touch SQL Server.
/// </summary>
public sealed class StoredFile
{
    /// <summary>
    /// Provider-opaque locator - a relative path locally, a blob name in Azure, a key
    /// in S3. Only the provider that produced it may interpret it.
    /// </summary>
    public required string StorageKey { get; init; }

    public required StorageProvider Provider { get; init; }

    public required string OriginalFileName { get; init; }

    /// <summary>Content type determined from the file's own bytes, not from the client.</summary>
    public required string ContentType { get; init; }

    public required string Extension { get; init; }

    public long SizeBytes { get; init; }

    /// <summary>Lowercase hex SHA-256 of the stored content. Used for de-duplication.</summary>
    public required string ChecksumSha256 { get; init; }
}
