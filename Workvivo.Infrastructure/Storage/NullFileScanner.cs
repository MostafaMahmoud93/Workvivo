using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Infrastructure.Storage;

/// <summary>
/// Approves every file. Registered by default so the upload pipeline has a scan step
/// from day one and adding a real engine is a DI swap rather than a pipeline change.
///
/// This is not virus protection. A deployment that accepts uploads from a large staff
/// should register a real scanner.
/// </summary>
public sealed class NullFileScanner : IFileScanner
{
    public Task<FileScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default) =>
        Task.FromResult(FileScanResult.Clean);
}
