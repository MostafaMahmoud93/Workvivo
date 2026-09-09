namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Injectable clock, so that "has this poll expired", "is this event tomorrow" and
/// "is this token still valid" are testable without waiting or freezing the machine.
///
/// New code stores and compares UTC. The template writes DateTime.Now in places;
/// that is left alone for now and unwound with its own migration.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateOnly TodayUtc { get; }
}
