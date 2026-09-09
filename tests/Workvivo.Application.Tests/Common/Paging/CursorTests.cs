using Shouldly;
using Workvivo.Application.Common.Paging;
using Xunit;

namespace Workvivo.Application.Tests.Common.Paging;

/// <summary>
/// The feed cursor is client-visible and therefore attacker-controlled. It has to
/// round-trip exactly and it has to fail closed on anything else.
/// </summary>
public class CursorTests
{
    [Fact]
    public void Round_trips_a_timestamp_and_id_without_loss()
    {
        var timestamp = new DateTime(2026, 9, 9, 14, 32, 17, DateTimeKind.Utc).AddTicks(4567);
        var id = Guid.NewGuid();

        var decoded = Cursor.TryDecode(Cursor.Encode(timestamp, id), out var outTimestamp, out var outId);

        decoded.ShouldBeTrue();
        // Tick-level fidelity matters: the cursor is compared against a datetime2
        // column, and a rounded value would skip or repeat rows at the page boundary.
        outTimestamp.ShouldBe(timestamp);
        outId.ShouldBe(id);
    }

    [Fact]
    public void Decoded_timestamps_are_UTC()
    {
        var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Cursor.TryDecode(Cursor.Encode(timestamp, Guid.NewGuid()), out var outTimestamp, out _);

        // An Unspecified kind would compare differently once a caller does any
        // conversion on it.
        outTimestamp.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-base64!!")]
    [InlineData("Zm9vOmJhcg==")]                 // decodes, but is not ticks:guid
    [InlineData("OTk5OTk5OTk5OTk5OTk5OTk5OTo=")] // ticks beyond DateTime.MaxValue
    public void Malformed_cursors_are_treated_as_no_cursor(string? cursor)
    {
        // Never throws. A hand-edited cursor should quietly return page one rather than
        // produce a 500 an attacker can use to probe the internals.
        Cursor.TryDecode(cursor, out _, out _).ShouldBeFalse();
    }
}
