namespace Workvivo.Domain.Abstractions.Classes;

/// <summary>
/// Builds a safe <c>LIKE</c> pattern from a user's search term.
///
/// SQL Server offers two ways to escape a wildcard and they cannot be mixed:
/// bracket-quoting (<c>[%]</c>, with no ESCAPE clause) or a declared escape
/// character (<c>\%</c> with <c>ESCAPE '\'</c>). Combining them - bracket-quoting
/// the term while also declaring <c>ESCAPE '['</c> - produces a pattern that
/// silently matches the wrong thing: a search for "50%" becomes a search for
/// "50%]" and returns nothing.
///
/// This picks the declared-escape form and states the character once, so the two
/// halves cannot drift apart.
///
/// Not a SQL-injection defence - the term is a parameter either way. It is a
/// correctness defence: without it, an ordinary term containing a percent sign or a
/// bracket quietly returns wrong results, and an underscore matches any character.
/// </summary>
public static class LikePattern
{
    /// <summary>The escape character, which every call site must declare to the provider.</summary>
    public const string EscapeCharacter = "\\";

    /// <summary>A "contains" pattern for the given term.</summary>
    public static string Contains(string term) => $"%{Escape(term)}%";

    /// <summary>A "starts with" pattern for the given term.</summary>
    public static string StartsWith(string term) => $"{Escape(term)}%";

    /// <summary>
    /// Neutralises the three characters LIKE treats specially.
    ///
    /// The escape character itself goes first - doing it last would escape the
    /// backslashes this method just inserted.
    /// </summary>
    public static string Escape(string term) => term
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_")
        .Replace("[", "\\[");
}
