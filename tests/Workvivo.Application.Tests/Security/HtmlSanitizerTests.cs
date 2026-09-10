using Shouldly;
using Workvivo.Infrastructure.Common;
using Xunit;

namespace Workvivo.Application.Tests.Security;

/// <summary>
/// Stored cross-site scripting is the highest-value attack against a product like this:
/// one post reaches every colleague who opens the feed.
///
/// These exist because of a real defect. The sanitiser was configured by passing an
/// <c>HtmlSanitizerOptions</c> instance, which replaces *every* default set - including
/// <c>UriAttributes</c>, which the options object leaves empty. With no attribute
/// registered as a URI the scheme allow-list was never consulted, and
/// <c>href="javascript:alert(1)"</c> passed through untouched. The configuration read
/// correctly and did nothing.
///
/// The fix starts from the library's defaults and narrows them. These tests pin the
/// behaviour rather than the configuration, so a future rewrite that reintroduces the
/// same mistake fails here.
/// </summary>
public class HtmlSanitizerTests
{
    private readonly HtmlSanitizerAdapter _sanitizer = new();

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<SCRIPT>alert(1)</SCRIPT>")]
    [InlineData("<scr<script>ipt>alert(1)</script>")]
    public void Script_elements_are_removed(string hostile)
    {
        _sanitizer.Sanitize(hostile).ShouldNotContain("alert", Case.Insensitive);
    }

    [Theory]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<div onmouseover=\"alert(1)\">hover</div>")]
    [InlineData("<body onload=alert(1)>")]
    public void Event_handler_attributes_are_removed(string hostile)
    {
        var clean = _sanitizer.Sanitize(hostile);

        clean.ShouldNotContain("onerror", Case.Insensitive);
        clean.ShouldNotContain("onmouseover", Case.Insensitive);
        clean.ShouldNotContain("onload", Case.Insensitive);
    }

    [Theory]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
    [InlineData("<a href=\"JaVaScRiPt:alert(1)\">x</a>")]
    [InlineData("<img src=\"javascript:alert(1)\">")]
    public void Javascript_urls_are_removed(string hostile)
    {
        // The exact defect: this passed before the sanitiser was rebuilt from defaults.
        _sanitizer.Sanitize(hostile).ShouldNotContain("javascript:", Case.Insensitive);
    }

    [Fact]
    public void Data_urls_are_removed()
    {
        // A data: URL can carry an SVG, and an SVG can carry script.
        var clean = _sanitizer.Sanitize(
            "<img src=\"data:image/svg+xml;base64,PHN2Zz48c2NyaXB0PmFsZXJ0KDEpPC9zY3JpcHQ+PC9zdmc+\">");

        clean.ShouldNotContain("data:", Case.Insensitive);
    }

    [Theory]
    [InlineData("<iframe src=\"https://evil.example\"></iframe>")]
    [InlineData("<object data=\"x\"></object>")]
    [InlineData("<embed src=\"x\">")]
    [InlineData("<form action=\"https://evil.example\"><input name=\"p\"></form>")]
    public void Framing_and_form_elements_are_removed(string hostile)
    {
        var clean = _sanitizer.Sanitize(hostile);

        clean.ShouldNotContain("<iframe", Case.Insensitive);
        clean.ShouldNotContain("<object", Case.Insensitive);
        clean.ShouldNotContain("<embed", Case.Insensitive);
        clean.ShouldNotContain("<form", Case.Insensitive);
    }

    [Fact]
    public void Ordinary_formatting_survives()
    {
        // A sanitiser that strips everything is safe and useless. The product needs
        // formatted text, so the allow-list has to keep working.
        var clean = _sanitizer.Sanitize(
            "<p>Hello <strong>team</strong>, see the <em>notes</em>.</p><ul><li>one</li></ul>");

        clean.ShouldContain("<strong>team</strong>");
        clean.ShouldContain("<em>notes</em>");
        clean.ShouldContain("<li>one</li>");
    }

    [Fact]
    public void Safe_links_survive_and_gain_noopener()
    {
        var clean = _sanitizer.Sanitize("<a href=\"https://example.com\">docs</a>");

        clean.ShouldContain("https://example.com");

        // Without noopener the opened page can reach back through window.opener.
        clean.ShouldContain("noopener");
        clean.ShouldContain("noreferrer");
    }

    [Fact]
    public void Mention_markup_survives()
    {
        // Mentions round-trip as a span carrying the employee id, so the client can turn
        // one into a profile link without re-parsing the text.
        var clean = _sanitizer.Sanitize(
            "<span data-mention-type=\"employee\" data-mention-id=\"11111111-1111-1111-1111-111111111111\">Layla</span>");

        clean.ShouldContain("data-mention-id");
        clean.ShouldContain("Layla");
    }

    [Fact]
    public void Plain_text_extraction_does_not_leak_script_bodies()
    {
        // Stripping tags with a regex alone would unwrap the script element and leave
        // its body behind as ordinary "text" - which then reaches search indexes and
        // digest emails.
        var text = _sanitizer.ToPlainText("<p>Hello</p><script>alert('stolen')</script>");

        text.ShouldContain("Hello");
        text.ShouldNotContain("alert");
        text.ShouldNotContain("stolen");
    }

    [Fact]
    public void Plain_text_extraction_decodes_entities_and_collapses_whitespace()
    {
        _sanitizer.ToPlainText("<p>a &amp; b</p>\n\n<p>   c   </p>").ShouldBe("a & b c");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_input_yields_an_empty_string_rather_than_null(string? input)
    {
        _sanitizer.Sanitize(input).ShouldBe(string.Empty);
        _sanitizer.ToPlainText(input).ShouldBe(string.Empty);
    }
}
