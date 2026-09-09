using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Workvivo.Domain.Abstractions.Interfaces;

namespace Workvivo.Infrastructure.Common;

/// <summary>
/// Wraps Ganss.Xss with the allow-list this product actually needs.
///
/// The allow-list is deliberately narrow: posts and comments are formatted text with
/// links and images, not documents. Every tag added here is a tag an attacker gets to
/// use, so the default answer to "can we allow &lt;iframe&gt;" is no.
/// </summary>
public sealed partial class HtmlSanitizerAdapter : IContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerAdapter()
    {
        _sanitizer = new HtmlSanitizer(new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "p", "br", "strong", "b", "em", "i", "u", "s", "blockquote",
                "ul", "ol", "li", "h1", "h2", "h3", "h4",
                "a", "img", "code", "pre", "hr", "span", "div", "table", "thead",
                "tbody", "tr", "th", "td",
            },
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "href", "title", "alt", "src", "width", "height", "class",
                // Mentions are rendered as a span carrying the employee id, so the
                // client can turn one into a profile link without re-parsing text.
                "data-mention-id", "data-mention-type",
            },
            AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "text-align", "direction",
            },
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // No "javascript", and no "data" - a data: URL can carry an SVG, and an
                // SVG can carry script.
                "http", "https", "mailto",
            },
            AllowedAtRules = new HashSet<AngleSharp.Css.Dom.CssRuleType>(),
        });

        // Outbound links open in a new tab; noopener stops the opened page reaching
        // back through window.opener, and noreferrer keeps internal URLs out of
        // third-party referer logs.
        _sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is AngleSharp.Html.Dom.IHtmlAnchorElement anchor
                && !string.IsNullOrEmpty(anchor.GetAttribute("href")))
            {
                anchor.SetAttribute("target", "_blank");
                anchor.SetAttribute("rel", "noopener noreferrer");
            }
        };
    }

    public string Sanitize(string? html) =>
        string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html);

    public string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        // Sanitise first: stripping tags from raw input with a regex would happily
        // unwrap a script element and leave its body as "text".
        var clean = _sanitizer.Sanitize(html);
        var withoutTags = TagPattern().Replace(clean, " ");

        return WhitespacePattern().Replace(WebUtility.HtmlDecode(withoutTags), " ").Trim();
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespacePattern();
}
