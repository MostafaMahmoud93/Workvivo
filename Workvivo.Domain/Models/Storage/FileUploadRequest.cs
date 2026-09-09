namespace Workvivo.Domain.Models.Storage;

/// <summary>
/// Everything the storage layer needs to persist one file, expressed without any
/// reference to ASP.NET types beyond the stream, so the same request can come from
/// an HTTP upload, a background job, or a test.
/// </summary>
public sealed class FileUploadRequest
{
    public required Stream Content { get; init; }

    /// <summary>
    /// The name the user's browser sent. Kept for display only - it is never used to
    /// build a path, because a caller controls it and can put "../" in it.
    /// </summary>
    public required string OriginalFileName { get; init; }

    /// <summary>Content type the client declared. Verified against the bytes before use.</summary>
    public required string DeclaredContentType { get; init; }

    public long Length { get; init; }

    /// <summary>
    /// Logical container: "posts", "avatars", "documents". Maps to a folder locally
    /// and to a blob prefix in the cloud. Constrained to an allow-list by the storage
    /// service so it cannot be used to escape the root.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>What kind of file this is expected to be, which picks the validation rules.</summary>
    public FileCategory Category { get; init; } = FileCategory.Document;
}

/// <summary>
/// Selects an upload policy - allowed extensions, size ceiling, whether images get
/// re-encoded. Never inferred from the file name.
/// </summary>
public enum FileCategory
{
    Image = 0,
    Video = 1,
    Document = 2,
    Avatar = 3,
}
