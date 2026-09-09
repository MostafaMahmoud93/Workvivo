namespace Workvivo.Infrastructure.Storage;

/// <summary>
/// A seekable buffer that starts in memory and spills to a temporary file once it
/// grows past a threshold.
///
/// Needed because the upload pipeline reads the content three times - signature, scan,
/// hash - and an HTTP request body is forward-only. Buffering everything in memory
/// would let a handful of concurrent video uploads exhaust the heap.
/// </summary>
internal sealed class FileBufferingStream : Stream
{
    private readonly int _memoryThreshold;
    private Stream _inner;
    private string? _tempFilePath;

    public FileBufferingStream(int memoryThreshold)
    {
        _memoryThreshold = memoryThreshold;
        _inner = new MemoryStream();
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => true;

    public override bool CanWrite => _inner.CanWrite;

    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value) => _inner.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        SpillIfNeeded(count);
        _inner.Write(buffer, offset, count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        SpillIfNeeded(buffer.Length);
        await _inner.WriteAsync(buffer, cancellationToken);
    }

    private void SpillIfNeeded(int incoming)
    {
        if (_tempFilePath is not null || _inner.Length + incoming <= _memoryThreshold)
        {
            return;
        }

        _tempFilePath = Path.Combine(Path.GetTempPath(), $"wv-upload-{Guid.NewGuid():N}.tmp");

        var file = new FileStream(
            _tempFilePath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        _inner.Position = 0;
        _inner.CopyTo(file);
        _inner.Dispose();
        _inner = file;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // FileOptions.DeleteOnClose removes the temp file; this is belt and braces
            // for the case where the process died before the handle was closed cleanly.
            _inner.Dispose();

            if (_tempFilePath is not null && File.Exists(_tempFilePath))
            {
                try
                {
                    File.Delete(_tempFilePath);
                }
                catch (IOException)
                {
                    // Another handle still holds it; the OS reclaims it on close.
                }
            }
        }

        base.Dispose(disposing);
    }
}
