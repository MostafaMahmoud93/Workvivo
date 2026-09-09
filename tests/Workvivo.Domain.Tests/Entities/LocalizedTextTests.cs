using System.Globalization;
using Shouldly;
using Workvivo.Domain.Abstractions.Classes;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

/// <summary>
/// Every bilingual entity resolves its display text through this, so the fallback
/// rule is stated once. These tests pin that rule.
/// </summary>
public class LocalizedTextTests
{
    private static T WithCulture<T>(string culture, Func<T> action)
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            return action();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void A_right_to_left_culture_gets_the_arabic_side()
    {
        WithCulture("ar-AE", () => LocalizedText.Pick("مرحبا", "Hello")).ShouldBe("مرحبا");
    }

    [Fact]
    public void A_left_to_right_culture_gets_the_english_side()
    {
        WithCulture("en-US", () => LocalizedText.Pick("مرحبا", "Hello")).ShouldBe("Hello");
    }

    [Fact]
    public void A_missing_english_value_falls_back_to_arabic()
    {
        // The Arabic column is the required one on most entities, so an English reader
        // seeing Arabic beats an English reader seeing a blank.
        WithCulture("en-US", () => LocalizedText.Pick("مرحبا", null)).ShouldBe("مرحبا");
    }

    [Fact]
    public void A_missing_arabic_value_falls_back_to_english()
    {
        WithCulture("ar-AE", () => LocalizedText.Pick(null, "Hello")).ShouldBe("Hello");
    }

    [Fact]
    public void Whitespace_counts_as_missing()
    {
        // An imported record with "   " in a column should not render as a blank label.
        WithCulture("en-US", () => LocalizedText.Pick("مرحبا", "   ")).ShouldBe("مرحبا");
    }

    [Fact]
    public void Both_sides_missing_yields_null()
    {
        WithCulture("en-US", () => LocalizedText.Pick(null, null)).ShouldBeNull();
    }
}
