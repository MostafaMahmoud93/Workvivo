namespace Workvivo.Infrastructure.Storage;

/// <summary>
/// Identifies a file from its leading bytes.
///
/// This is the check that actually matters. An extension and a Content-Type header
/// are both supplied by the caller, so "shell.aspx" renamed to "photo.png" passes an
/// extension check and a MIME check and still executes if it ever lands somewhere
/// that runs it. The bytes are the only part the caller cannot lie about while still
/// having the file work as the type they claim.
/// </summary>
public static class FileSignatureValidator
{
    // Ordered longest-prefix-first within each extension so a more specific magic
    // number wins over a shorter one that also matches.
    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".gif"] = [
            [0x47, 0x49, 0x46, 0x38, 0x37, 0x61],
            [0x47, 0x49, 0x46, 0x38, 0x39, 0x61],
        ],
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46, 0x2D]],
        // The modern Office formats and .zip are all ZIP containers, so they share
        // the PK signature. Telling them apart needs the archive's manifest, which is
        // more than this gate is for: the extension allow-list already decided the
        // file may be a ZIP-shaped thing.
        [".zip"] = [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]],
        [".docx"] = [[0x50, 0x4B, 0x03, 0x04]],
        [".xlsx"] = [[0x50, 0x4B, 0x03, 0x04]],
        [".pptx"] = [[0x50, 0x4B, 0x03, 0x04]],
        [".doc"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        [".xls"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        [".ppt"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        [".mp4"] = [[0x66, 0x74, 0x79, 0x70]], // at offset 4
        [".mov"] = [[0x66, 0x74, 0x79, 0x70]], // at offset 4
        [".webm"] = [[0x1A, 0x45, 0xDF, 0xA3]],
    };

    // Offset at which the signature appears, where it is not zero.
    private static readonly Dictionary<string, int> Offsets = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp4"] = 4,
        [".mov"] = 4,
    };

    /// <summary>
    /// Extensions whose content is text and has no magic number. Allowed through the
    /// signature gate; the extension allow-list and the size cap still apply.
    /// </summary>
    private static readonly HashSet<string> Signatureless =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".csv", ".webp" };

    /// <summary>
    /// True when the content's leading bytes are consistent with the extension.
    /// Leaves the stream's position where it found it.
    /// </summary>
    public static async Task<bool> MatchesAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        if (Signatureless.Contains(extension))
        {
            return true;
        }

        if (!Signatures.TryGetValue(extension, out var candidates))
        {
            // Unknown extension. The allow-list should already have rejected it; if it
            // reached here, refuse rather than wave it through.
            return false;
        }

        var offset = Offsets.GetValueOrDefault(extension, 0);
        var longest = candidates.Max(c => c.Length);
        var needed = offset + longest;

        if (!content.CanSeek)
        {
            throw new ArgumentException("Signature validation needs a seekable stream.", nameof(content));
        }

        var origin = content.Position;
        try
        {
            var buffer = new byte[needed];
            content.Position = 0;
            var read = await content.ReadAtLeastAsync(buffer, needed, throwOnEndOfStream: false, cancellationToken);

            return read >= needed
                && candidates.Any(signature => buffer.AsSpan(offset, signature.Length).SequenceEqual(signature));
        }
        finally
        {
            content.Position = origin;
        }
    }

    /// <summary>Canonical content type for an extension, used instead of the client's claim.</summary>
    public static string ResolveContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".ppt" => "application/vnd.ms-powerpoint",
        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        ".zip" => "application/zip",
        ".mp4" => "video/mp4",
        ".mov" => "video/quicktime",
        ".webm" => "video/webm",
        _ => "application/octet-stream",
    };
}
