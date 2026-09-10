using System.Text.Json;
using System.Text.Json.Serialization;

namespace Workvivo.API.Serialization;

/// <summary>
/// Serialises every <see cref="DateTime"/> as UTC, with the trailing <c>Z</c>.
///
/// Without this, a value read from a <c>datetime2</c> column comes back with
/// <see cref="DateTimeKind.Unspecified"/>, and <c>System.Text.Json</c> writes it with no
/// offset at all - <c>"2026-09-10T04:41:10.37"</c>. Every browser then parses that as
/// *local* time. On a machine four hours ahead of UTC, a notification created one minute
/// ago renders as "4h ago", and a post published this morning sorts as though it were
/// published this afternoon. Nothing errors; the times are simply wrong, everywhere, by
/// exactly the client's offset.
///
/// The convention this enforces is that the database stores UTC. That is now true
/// throughout - the audit stamping in the DbContext was changed from
/// <c>DateTime.Now</c> to <c>DateTime.UtcNow</c> for the same reason.
///
/// Reading is symmetrical: a value with an offset is converted, one without is taken at
/// its word as UTC.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        // Round-trip format, which is what Date.parse and every ISO-8601 reader expect.
        writer.WriteStringValue(utc.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// The nullable counterpart. Registering only the non-nullable converter leaves
/// <c>DateTime?</c> properties - which is most of them - going out without the offset.
/// </summary>
public sealed class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly UtcDateTimeConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : Inner.Read(ref reader, typeof(DateTime), options);

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        Inner.Write(writer, value.Value, options);
    }
}
