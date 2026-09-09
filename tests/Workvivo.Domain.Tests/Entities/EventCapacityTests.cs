using Shouldly;
using Workvivo.Domain.Entities.Events;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

public class EventCapacityTests
{
    [Fact]
    public void An_event_with_no_capacity_never_fills_up()
    {
        new Event { Capacity = null, Attendees_Count = 10_000 }
            .HasCapacityRemaining.ShouldBeTrue();
    }

    [Fact]
    public void Room_remains_below_capacity()
    {
        new Event { Capacity = 50, Attendees_Count = 49 }.HasCapacityRemaining.ShouldBeTrue();
    }

    [Fact]
    public void A_full_event_has_no_room()
    {
        // The boundary: at exactly capacity the event is full, not "one more allowed".
        new Event { Capacity = 50, Attendees_Count = 50 }.HasCapacityRemaining.ShouldBeFalse();
    }

    [Fact]
    public void An_over_subscribed_event_has_no_room()
    {
        new Event { Capacity = 50, Attendees_Count = 51 }.HasCapacityRemaining.ShouldBeFalse();
    }
}
