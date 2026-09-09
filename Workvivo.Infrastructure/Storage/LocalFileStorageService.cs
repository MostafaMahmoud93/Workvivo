using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Workvivo.Domain.Exceptions;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Models.Storage;

namespace Workvivo.Infrastructure.Storage;

/// <summary>
/// Stores files on the local filesystem or a mounted share. The default provider, and
/// the one used in development and single-node deployments.
///
/// Layout is {root}/{container}/{yyyy}/{MM}/{guid}{ext}. Date sharding keeps any one
/// directory small - a flat folder with a million entries is slow to enumerate on
/// every filesystem worth naming.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private static readonly string[] AllowedContainers =
        ["posts", "avatars", "covers", "documents", "communities", "events", "recognition", "temp"];

    private readonly StorageOptions _options;
    private readonly FileValidationOptions _validation;
    private readonly IFileScanner _scanner;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IOptions<StorageOptions> options,
        IOptions<FileValidationOptions> validation,
        IFileScanner scanner,
        ILogger<LocalFileStorageService> logger)
    {
        _options = options.Value;
        _validation = validation.Value;
        _scanner = scanner;
        _logger = logger;
    }

    public string Provider => nameof(StorageProvider.Local);

    public async Task<StoredFile> SaveAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        var container = NormaliseContainer(request.Container);
        var extension = Path.GetExtension(request.OriginalFileName)?.ToLowerInvariant() ?? string.Empty;
        var policy = _validation.For(request.Category);

        if (extension.Length == 0 || !policy.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(
                $"Files of type '{extension}' are not accepted here.",
                "file.extension-not-allowed");
        }

        // SVG is markup that browsers execute. It is never on an allow-list above, but
        // the check is repeated here so a future configuration edit cannot open the
        // hole by accident.
        if (extension is ".svg" or ".svgz")
        {
            throw new BusinessRuleException("SVG files are not accepted.", "file.svg-not-allowed");
        }

        var content = await EnsureSeekableAsync(request.Content, cancellationToken);

        try
        {
            if (content.Length == 0)
            {
                throw new BusinessRuleException("The file is empty.", "file.empty");
            }

            if (content.Length > policy.MaxSizeBytes)
            {
                throw new BusinessRuleException(
                    $"The file exceeds the {policy.MaxSizeBytes / (1024 * 1024)} MB limit for this type.",
                    "file.too-large");
            }

            if (!await FileSignatureValidator.MatchesAsync(content, extension, cancellationToken))
            {
                _logger.LogWarning(
                    "Rejected upload {FileName}: content does not match its {Extension} extension",
                    request.OriginalFileName,
                    extension);

                throw new BusinessRuleException(
                    "The file contents do not match its type.",
                    "file.signature-mismatch");
            }

            var scan = await _scanner.ScanAsync(content, request.OriginalFileName, cancellationToken);
            if (!scan.IsClean)
            {
                _logger.LogWarning("Rejected upload {FileName}: {Threat}", request.OriginalFileName, scan.Threat);
                throw new BusinessRuleException("The file failed a security scan.", "file.scan-failed");
            }

            content.Position = 0;
            var checksum = Convert.ToHexStringLower(await SHA256.HashDataAsync(content, cancellationToken));

            // The stored name is generated, never derived from user input: the original
            // name can contain path separators, traversal sequences, reserved Windows
            // device names, or a second extension.
            var now = DateTime.UtcNow;
            var relativeDirectory = Path.Combine(container, now.ToString("yyyy"), now.ToString("MM"));
            var storageKey = Path.Combine(relativeDirectory, $"{Guid.NewGuid():N}{extension}").Replace('\\', '/');

            var absoluteDirectory = Path.Combine(Root, relativeDirectory);
            Directory.CreateDirectory(absoluteDirectory);

            var absolutePath = ResolveAbsolute(storageKey);
            content.Position = 0;

            await using (var destination = new FileStream(
                absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await content.CopyToAsync(destination, cancellationToken);
            }

            _logger.LogInformation(
                "Stored {StorageKey} ({SizeBytes} bytes) in container {Container}",
                storageKey,
                content.Length,
                container);

            return new StoredFile
            {
                StorageKey = storageKey,
                Provider = StorageProvider.Local,
                OriginalFileName = Path.GetFileName(request.OriginalFileName),
                ContentType = FileSignatureValidator.ResolveContentType(extension),
                Extension = extension,
                SizeBytes = content.Length,
                ChecksumSha256 = checksum,
            };
        }
        finally
        {
            if (!ReferenceEquals(content, request.Content))
            {
                await content.DisposeAsync();
            }
        }
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolveAbsolute(storageKey);

        if (!File.Exists(path))
        {
            throw new NotFoundException("File", storageKey);
        }

        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);

        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(ResolveAbsolute(storageKey)));

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolveAbsolute(storageKey);

        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogInformation("Deleted {StorageKey}", storageKey);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Local disk has no presigned-URL concept. Returning null tells the caller to
    /// stream through the authorised download endpoint instead.
    /// </summary>
    public Task<Uri?> GetPresignedUrlAsync(string storageKey, TimeSpan lifetime, CancellationToken cancellationToken = default) =>
        Task.FromResult<Uri?>(null);

    private string Root =>
        string.IsNullOrWhiteSpace(_options.LocalRootPath)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "storage")
            : _options.LocalRootPath;

    private static string NormaliseContainer(string container)
    {
        var trimmed = container?.Trim().ToLowerInvariant() ?? string.Empty;

        // An allow-list rather than sanitisation: the container ends up in a path, and
        // the set of legitimate values is small and known.
        if (!AllowedContainers.Contains(trimmed))
        {
            throw new BusinessRuleException($"Unknown storage container '{container}'.", "file.unknown-container");
        }

        return trimmed;
    }

    /// <summary>
    /// Turns a storage key into an absolute path and proves the result is still inside
    /// the storage root. Without this a key of "../../appsettings.json" reads whatever
    /// the process can reach.
    /// </summary>
    private string ResolveAbsolute(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new NotFoundException("File", storageKey ?? "(null)");
        }

        var root = Path.GetFullPath(Root);
        var candidate = Path.GetFullPath(Path.Combine(root, storageKey));

        if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(candidate, root, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Blocked path traversal attempt for storage key {StorageKey}", storageKey);
            throw new ForbiddenException("Invalid file reference.");
        }

        return candidate;
    }

    /// <summary>
    /// Signature checking and hashing both need to rewind. A multipart upload stream
    /// often cannot, so it is buffered - to memory when small, to a temp file when not,
    /// so a large video upload does not sit in the managed heap.
    /// </summary>
    private static async Task<Stream> EnsureSeekableAsync(Stream source, CancellationToken cancellationToken)
    {
        const int memoryThreshold = 4 * 1024 * 1024;

        if (source.CanSeek)
        {
            source.Position = 0;
            return source;
        }

        var buffer = new FileBufferingStream(memoryThreshold);
        await source.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }
}
