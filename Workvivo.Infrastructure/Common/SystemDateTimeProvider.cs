using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Infrastructure.Common;

/// <summary>The real clock. Tests substitute a fixed one.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);
}
