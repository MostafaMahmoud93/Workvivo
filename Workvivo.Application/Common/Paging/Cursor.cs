using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Workvivo.Application.Common.Paging;

/// <summary>
/// Encodes and decodes the keyset cursor used by every timeline query.
///
/// The payload is "{timestamp ticks}:{tiebreaker id}". The timestamp alone is not
/// unique - two posts published in the same tick would make a row either repeat on
/// the next page or vanish entirely - so the primary key rides along as a tiebreaker
/// and the ORDER BY matches the cursor exactly.
///
/// Base64url only obscures the value; it is not a security boundary, and callers can
/// forge one. Decoding is therefore total: a malformed or hand-edited cursor means
/// "no cursor" and the caller gets page one, rather than an exception they could farm
/// for detail about the internals.
/// </summary>
public static class Cursor
{
    public static string Encode(DateTime timestamp, Guid id)
    {
        var raw = string.Create(CultureInfo.InvariantCulture, $"{timestamp.Ticks}:{id:N}");
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(raw));
    }

    public static bool TryDecode(string? cursor, out DateTime timestamp, out Guid id)
    {
        timestamp = default;
        id = default;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        try
        {
            var raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(cursor));
            var separator = raw.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                return false;
            }

            if (!long.TryParse(raw.AsSpan(0, separator), CultureInfo.InvariantCulture, out var ticks)
                || ticks < DateTime.MinValue.Ticks
                || ticks > DateTime.MaxValue.Ticks)
            {
                return false;
            }

            if (!Guid.TryParseExact(raw.AsSpan(separator + 1), "N", out var parsedId))
            {
                return false;
            }

            timestamp = new DateTime(ticks, DateTimeKind.Utc);
            id = parsedId;
            return true;
        }
        catch (FormatException)
        {
            // Not valid base64url. Treated as "start from the beginning".
            return false;
        }
    }
}
