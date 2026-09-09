using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Documents;

/// <summary>
/// Metadata for one physical file. The single registry every upload in the system
/// goes through - avatars, post attachments, document versions, event banners.
///
/// The bytes are never here. They live behind IFileStorageService, and this row holds
/// the opaque key that fetches them. Storing binaries in SQL Server would bloat the
/// database, wreck backup and restore times, and force every read through the
/// database connection pool.
/// </summary>
public class FileAsset : BaseCommonEntity<Guid>
{
    /// <summary>Which provider holds the bytes - only that provider can read the key.</summary>
    public StorageProvider Storage_Provider { get; set; }

    /// <summary>Provider-opaque locator: a relative path, a blob name, an S3 key.</summary>
    public string Storage_Key { get; set; } = string.Empty;

    /// <summary>
    /// The name the browser sent, kept for display and for the download filename.
    /// Never used to build a path - a caller controls it, and can put traversal
    /// sequences or reserved device names in it.
    /// </summary>
    public string Original_File_Name { get; set; } = string.Empty;

    /// <summary>
    /// Content type determined from the file's own bytes, not from what the client
    /// claimed. What the browser is told on download.
    /// </summary>
    public string Content_Type { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;
    public long Size_Bytes { get; set; }

    /// <summary>
    /// SHA-256 of the content, lowercase hex. Used to recognise a file that has
    /// already been uploaded and to detect corruption in the store.
    /// </summary>
    public string Checksum_Sha256 { get; set; } = string.Empty;

    public FileScanStatus Scan_Status { get; set; } = FileScanStatus.Pending;
    public DateTime? Scanned_At { get; set; }

    public Guid Uploaded_By { get; set; }
    public DateTime Uploaded_At { get; set; }

    /// <summary>
    /// Logical container - "posts", "avatars", "documents". Constrained to an
    /// allow-list by the storage service.
    /// </summary>
    public string Container { get; set; } = string.Empty;

    public virtual ApplicationUser? UploadedBy { get; set; }

    /// <summary>True when the file has been scanned and found clean.</summary>
    public bool IsServable => !Is_Deleted && Scan_Status is FileScanStatus.Clean or FileScanStatus.Skipped;
}
