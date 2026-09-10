using Shouldly;
using Workvivo.Domain.Entities.Communities;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

/// <summary>
/// A slug is a community's permanent link. It has to be safe in a URL, stable, and
/// unique - and it is built from names that people write in two scripts with
/// arbitrary punctuation.
/// </summary>
public class CommunitySlugTests
{
    [Theory]
    [InlineData("Football Club", "football-club")]
    [InlineData("  Football   Club  ", "football-club")]
    [InlineData("Football Club!", "football-club")]
    [InlineData("R&D", "r-d")]
    [InlineData("Café Society", "cafe-society")]
    public void Punctuation_spacing_and_accents_collapse_to_something_typeable(
        string name, string expected)
    {
        CommunitySlug.From(name, "ignored").ShouldBe(expected);
    }

    [Fact]
    public void An_arabic_only_name_still_produces_a_usable_slug()
    {
        // Arabic in a path works but yields a link nobody can type or read aloud, and
        // romanising it properly needs a transliteration table. The fallback is a
        // generic slug that the caller will disambiguate with a suffix.
        var slug = CommunitySlug.From(null, "نادي كرة القدم");

        slug.ShouldNotBeNullOrWhiteSpace();
        CommunitySlug.IsValid(slug).ShouldBeTrue();
    }

    [Fact]
    public void The_english_name_wins_when_both_are_present()
    {
        CommunitySlug.From("Design Guild", "مجلس التصميم").ShouldBe("design-guild");
    }

    [Fact]
    public void A_suffix_disambiguates_without_breaking_the_slug()
    {
        var slug = CommunitySlug.From("Football Club", null, "a1b2c3");

        slug.ShouldBe("football-club-a1b2c3");
        CommunitySlug.IsValid(slug).ShouldBeTrue();
    }

    [Fact]
    public void A_long_name_is_truncated_to_fit_the_column()
    {
        var slug = CommunitySlug.From(new string('a', 500), null);

        slug.Length.ShouldBeLessThanOrEqualTo(CommunitySlug.MaxLength);
    }

    [Fact]
    public void A_long_name_with_a_suffix_still_fits()
    {
        // The case that would otherwise overflow: truncation has to account for the
        // suffix it is about to append, not just the base name.
        var slug = CommunitySlug.From(new string('a', 500), null, "a1b2c3");

        slug.Length.ShouldBeLessThanOrEqualTo(CommunitySlug.MaxLength);
        slug.ShouldEndWith("-a1b2c3");
    }

    [Fact]
    public void A_slug_never_starts_or_ends_with_a_separator()
    {
        // "/communities/-football-" is a link that works and looks broken.
        CommunitySlug.From("  !Football!  ", null).ShouldBe("football");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Not A Slug")]
    [InlineData("trailing-")]
    [InlineData("UPPER")]
    public void Validation_rejects_anything_that_is_not_already_a_slug(string? candidate)
    {
        CommunitySlug.IsValid(candidate).ShouldBeFalse();
    }
}
