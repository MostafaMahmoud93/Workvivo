namespace Workvivo.Domain.Abstractions.Enums;

/// <summary>
/// Result of the malware scan on an uploaded file.
///
/// <see cref="Pending"/> is the default so a file is never treated as safe merely
/// because no scanner has looked at it yet.
/// </summary>
public enum FileScanStatus
{
    Pending = 0,
    Clean = 1,
    Infected = 2,
    Failed = 3,
    Skipped = 4,
}
