using Shouldly;
using Workvivo.Domain.Abstractions.Classes;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

/// <summary>
/// These exist because of a real defect. Search escaped its term with SQL Server's
/// bracket form - <c>[%]</c> - while also declaring <c>ESCAPE '['</c>. The two
/// schemes cannot be combined: with that escape character, <c>[%</c> is a literal
/// percent sign and the trailing <c>]</c> is a literal bracket, so a search for
/// "50%" became a search for "50%]" and matched nothing.
///
/// Nothing threw. The results were simply wrong for any term containing a wildcard,
/// which is exactly the kind of failure nobody reports as a bug - they just decide
/// the search is bad.
/// </summary>
public class LikePatternTests
{
    [Fact]
    public void An_ordinary_term_is_wrapped_for_a_contains_match()
    {
        LikePattern.Contains("policy").ShouldBe("%policy%");
    }

    [Fact]
    public void A_percent_sign_is_escaped_rather_than_left_as_a_wildcard()
    {
        // Unescaped, "50%" matches every row beginning "50" - and "%" alone matches
        // the entire table.
        LikePattern.Contains("50%").ShouldBe("%50\\%%");
    }

    [Fact]
    public void An_underscore_is_escaped_rather_than_matching_any_character()
    {
        // The quiet one: "a_b" silently matches "axb" without this.
        LikePattern.Contains("a_b").ShouldBe("%a\\_b%");
    }

    [Fact]
    public void A_bracket_is_escaped()
    {
        LikePattern.Contains("a[b").ShouldBe("%a\\[b%");
    }

    [Fact]
    public void The_escape_character_itself_is_escaped_first()
    {
        // Order matters. Escaping backslashes last would also escape the ones this
        // method had just inserted, doubling them and breaking every other rule.
        LikePattern.Escape("a\\%b").ShouldBe("a\\\\\\%b");
    }

    [Fact]
    public void The_declared_escape_character_matches_what_the_pattern_uses()
    {
        // The two halves have to agree, and they are the two halves that drifted
        // apart in the original defect. A pattern escaped one way and declared
        // another is wrong in a way no exception reports.
        LikePattern.EscapeCharacter.ShouldBe("\\");
        LikePattern.Escape("%").ShouldStartWith(LikePattern.EscapeCharacter);
    }

    [Fact]
    public void A_starts_with_pattern_anchors_at_the_beginning()
    {
        LikePattern.StartsWith("Nad").ShouldBe("Nad%");
    }

    [Fact]
    public void An_empty_term_produces_a_match_everything_pattern()
    {
        // Stated rather than assumed: callers are responsible for rejecting a term
        // that is too short, and this makes it obvious what happens if they do not.
        LikePattern.Contains(string.Empty).ShouldBe("%%");
    }
}
