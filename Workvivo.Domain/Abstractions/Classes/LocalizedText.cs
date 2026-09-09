namespace Workvivo.Domain.Abstractions.Classes;

/// <summary>
/// Picks the Arabic or English side of a bilingual column pair for the current
/// request culture.
///
/// Every entity in the template exposes a [NotMapped] accessor that does this, each
/// with its own copy of the same ternary. Centralising it means the fallback rule -
/// Arabic when the culture is right-to-left, otherwise English, falling back to
/// Arabic when the English side was never filled in - is stated once instead of
/// forty times, and cannot drift between entities.
/// </summary>
public static class LocalizedText
{
    /// <summary>
    /// Returns the value matching the current culture's direction, falling back to
    /// the other side when the preferred one is missing.
    /// </summary>
    public static string? Pick(string? arabic, string? english)
    {
        if (Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft)
        {
            return string.IsNullOrWhiteSpace(arabic) ? english : arabic;
        }

        return string.IsNullOrWhiteSpace(english) ? arabic : english;
    }
}
