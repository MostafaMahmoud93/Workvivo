using Workvivo.Domain.Models.Storage;

namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// The only way application code touches file bytes.
///
/// Implementations exist for local disk, Azure Blob and S3; which one runs is a
/// configuration value, so moving a deployment to the cloud changes no application
/// code. Nothing here exposes a public URL: downloads always go through an
/// authorised endpoint or a short-lived presigned link.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Identifier of the active provider, persisted alongside the storage key.</summary>
    string Provider { get; }

    /// <summary>
    /// Validates and stores the content. Throws if the file fails the upload policy
    /// for its category - wrong extension, oversized, or bytes that do not match the
    /// declared content type.
    /// </summary>
    Task<StoredFile> SaveAsync(FileUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>Opens the stored content for reading. The caller owns the stream.</summary>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// A time-limited URL the browser can fetch directly, letting large downloads
    /// bypass the API process. Returns null when the provider has no such concept
    /// and the caller should stream through the authorised endpoint instead.
    /// </summary>
    Task<Uri?> GetPresignedUrlAsync(string storageKey, TimeSpan lifetime, CancellationToken cancellationToken = default);
}
