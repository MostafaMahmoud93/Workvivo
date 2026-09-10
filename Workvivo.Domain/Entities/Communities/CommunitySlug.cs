using System.Globalization;
using System.Text;

namespace Workvivo.Domain.Entities.Communities;

/// <summary>
/// Turns a community's name into the URL-safe identifier its permanent link uses.
///
/// Two things make this less trivial than it looks. Names are bilingual, and Arabic
/// characters in a path work but produce a link nobody can type or read aloud - so an
/// English name is preferred and Arabic is only used when there is nothing else.
/// And slugs must be unique, which the generator cannot guarantee on its own, so it
/// takes a disambiguating suffix from the caller that knows what is already taken.
/// </summary>
public static class CommunitySlug
{
    /// <summary>Bounded to the column width, leaving room for a suffix.</summary>
    public const int MaxLength = 80;

    /// <summary>
    /// Builds a slug from the two names.
    /// </summary>
    /// <param name="suffix">
    /// Appended when supplied, to break a collision. Callers pass nothing on the first
    /// attempt and a short discriminator afterwards.
    /// </param>
    public static string From(string? nameEn, string? nameAr, string? suffix = null)
    {
        var basis = Slugify(nameEn);

        if (basis.Length == 0)
        {
            // Arabic-only name. Latinising it properly needs a transliteration table
            // and still produces something arguable, so the honest answer is a
            // non-guessable stable id rather than a bad romanisation.
            basis = Slugify(nameAr).Length > 0 ? Slugify(nameAr) : "community";
        }

        if (string.IsNullOrWhiteSpace(suffix))
        {
            return Truncate(basis, MaxLength);
        }

        var clean = Slugify(suffix);

        return Truncate(basis, MaxLength - clean.Length - 1) + "-" + clean;
    }

    /// <summary>True when a slug is safe to put in a path and fits the column.</summary>
    public static bool IsValid(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && slug.Length <= MaxLength
        && slug == Slugify(slug);

    private static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Decompose first, so "Café" becomes "Cafe" rather than losing the letter.
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var lastWasSeparator = true;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                lastWasSeparator = false;
                continue;
            }

            // Runs of anything else collapse to one hyphen, and a leading one is
            // dropped - "  Football  Club!  " must not become "--football--club--".
            if (!lastWasSeparator)
            {
                builder.Append('-');
                lastWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..Math.Max(1, maxLength)].TrimEnd('-');
}
