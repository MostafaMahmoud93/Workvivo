using Workvivo.Domain.Models.Storage;

namespace Workvivo.Infrastructure.Storage;

/// <summary>
/// Upload policy per file category. Bound from "Storage:Validation" so a deployment
/// can tighten the rules without a rebuild - but the defaults are already the strict
/// ones, so a missing configuration section fails safe rather than open.
/// </summary>
public sealed class FileValidationOptions
{
    public const string SectionName = "Storage:Validation";

    public FileCategoryPolicy Image { get; set; } = new()
    {
        MaxSizeBytes = 10L * 1024 * 1024,
        AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"],
    };

    public FileCategoryPolicy Avatar { get; set; } = new()
    {
        MaxSizeBytes = 2L * 1024 * 1024,
        AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"],
    };

    public FileCategoryPolicy Video { get; set; } = new()
    {
        MaxSizeBytes = 512L * 1024 * 1024,
        AllowedExtensions = [".mp4", ".webm", ".mov"],
    };

    public FileCategoryPolicy Document { get; set; } = new()
    {
        MaxSizeBytes = 50L * 1024 * 1024,
        AllowedExtensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".zip"],
    };

    public FileCategoryPolicy For(FileCategory category) => category switch
    {
        FileCategory.Image => Image,
        FileCategory.Avatar => Avatar,
        FileCategory.Video => Video,
        _ => Document,
    };
}

public sealed class FileCategoryPolicy
{
    public long MaxSizeBytes { get; set; }

    /// <summary>
    /// An allow-list, always - a deny-list of "dangerous" extensions is a list of the
    /// ones somebody thought of, and the interesting attack is the one they did not.
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [];
}
