namespace Workvivo.Domain.Abstractions.Enums;

public enum EventType
{
    Company = 0,
    Team = 1,
    Community = 2,
    Training = 3,
    Social = 4,
}

/// <summary>
/// Where the event happens. Separate from <see cref="EventType"/> because a company
/// all-hands and a team stand-up can each be online, physical or both.
/// </summary>
public enum EventFormat
{
    Physical = 0,
    Online = 1,
    Hybrid = 2,
}

public enum EventStatus
{
    Draft = 0,
    Published = 1,
    Cancelled = 2,
    Completed = 3,
}

public enum EventResponse
{
    Attending = 0,
    Maybe = 1,
    Declined = 2,
}
