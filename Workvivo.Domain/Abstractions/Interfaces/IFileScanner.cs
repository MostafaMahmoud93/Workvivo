namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Malware scan hook, called before a file is committed to storage.
///
/// The default implementation is a no-op that approves everything - a placeholder so
/// wiring a real scanner (ICAP, Defender, ClamAV) later is a DI registration rather
/// than a change to the upload pipeline.
/// </summary>
public interface IFileScanner
{
    Task<FileScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}

public sealed record FileScanResult(bool IsClean, string? Threat = null)
{
    public static FileScanResult Clean { get; } = new(true);
}
