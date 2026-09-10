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
    private static readonly string[] Tags =
    [
        "p", "br", "strong", "b", "em", "i", "u", "s", "blockquote",
        "ul", "ol", "li", "h1", "h2", "h3", "h4",
        "a", "img", "code", "pre", "hr", "span", "div",
        "table", "thead", "tbody", "tr", "th", "td",
    ];

    private static readonly string[] Attributes =
    [
        "href", "title", "alt", "src", "width", "height", "class",

        // Mentions render as a span carrying the employee id, so the client can turn
        // one into a profile link without re-parsing the text.
        "data-mention-id", "data-mention-type",
    ];

    private static readonly string[] Schemes = ["http", "https", "mailto"];

    private static readonly string[] CssProperties = ["text-align", "direction"];

    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerAdapter()
    {
        // Built from the default constructor and then narrowed - never from an
        // HtmlSanitizerOptions instance.
        //
        // Passing options replaces *every* default set, including UriAttributes, which
        // an options object leaves empty. With no attribute registered as a URI, the
        // scheme allow-list is never consulted and href="javascript:alert(1)" survives
        // sanitisation untouched. The configuration looks correct and does nothing.
        //
        // Starting from the defaults keeps UriAttributes (15 of them) and anything else
        // the library considers dangerous that this list has not thought of.
        _sanitizer = new HtmlSanitizer();

        Replace(_sanitizer.AllowedTags, Tags);
        Replace(_sanitizer.AllowedAttributes, Attributes);
        Replace(_sanitizer.AllowedSchemes, Schemes);
        Replace(_sanitizer.AllowedCssProperties, CssProperties);

        // No @import, no @font-face - both fetch remote resources from inside a style
        // block, which is a data-exfiltration channel.
        _sanitizer.AllowedAtRules.Clear();

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
        // unwrap a script element and leave its body behind as "text".
        var clean = _sanitizer.Sanitize(html);
        var withoutTags = TagPattern().Replace(clean, " ");

        return WhitespacePattern().Replace(WebUtility.HtmlDecode(withoutTags), " ").Trim();
    }

    /// <summary>Narrows one of the library's default sets to exactly the values given.</summary>
    private static void Replace(ISet<string> target, IEnumerable<string> values)
    {
        target.Clear();

        foreach (var value in values)
        {
            target.Add(value);
        }
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespacePattern();
}
