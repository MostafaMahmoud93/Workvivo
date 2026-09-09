namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Strips anything executable from user-authored rich text.
///
/// Applied on write, not on read: sanitising at render time means the database holds
/// live payloads, and it only takes one consumer that forgets - an export, a digest
/// email, a mobile client - for them to fire. Storing only clean markup makes that
/// class of bug impossible.
/// </summary>
public interface IContentSanitizer
{
    /// <summary>Returns markup containing only allow-listed tags, attributes and URL schemes.</summary>
    string Sanitize(string? html);

    /// <summary>Strips all markup, for search indexes, previews and plain-text email.</summary>
    string ToPlainText(string? html);
}
