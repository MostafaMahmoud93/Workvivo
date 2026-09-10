using Workvivo.Domain.Abstractions.Enums;

namespace Workvivo.Application.Features.Notifications.Common;

/// <summary>
/// The words a notification is made of, in both languages.
///
/// Written at send time rather than rendered per reader. A notification is a record of
/// something that happened: if Layla changes her display name next month, "Layla
/// commented on your post" should still say Layla, because that is who commented. It
/// also means the digest email and the mobile client cannot disagree with the bell.
///
/// Names are interpolated, never translated. Everything around them is.
/// </summary>
public static class NotificationCopy
{
    /// <summary>Longest excerpt of user content quoted inside a notification body.</summary>
    private const int ExcerptLength = 140;

    public static Text CommentedOnYourPost(string actor, string? excerpt) => new(
        HeaderAr: $"علّق {actor} على منشورك",
        HeaderEn: $"{actor} commented on your post",
        ContentAr: Excerpt(excerpt),
        ContentEn: Excerpt(excerpt));

    public static Text RepliedToYourComment(string actor, string? excerpt) => new(
        HeaderAr: $"ردّ {actor} على تعليقك",
        HeaderEn: $"{actor} replied to your comment",
        ContentAr: Excerpt(excerpt),
        ContentEn: Excerpt(excerpt));

    public static Text MentionedYouInAPost(string actor, string? excerpt) => new(
        HeaderAr: $"أشار إليك {actor} في منشور",
        HeaderEn: $"{actor} mentioned you in a post",
        ContentAr: Excerpt(excerpt),
        ContentEn: Excerpt(excerpt));

    public static Text MentionedYouInAComment(string actor, string? excerpt) => new(
        HeaderAr: $"أشار إليك {actor} في تعليق",
        HeaderEn: $"{actor} mentioned you in a comment",
        ContentAr: Excerpt(excerpt),
        ContentEn: Excerpt(excerpt));

    public static Text ReactedToYourPost(string actor, ReactionType reaction) => new(
        HeaderAr: $"تفاعل {actor} مع منشورك",
        HeaderEn: $"{actor} reacted to your post",
        ContentAr: ReactionAr(reaction),
        ContentEn: ReactionEn(reaction));

    public static Text Announcement(string? titleAr, string? titleEn, string? excerpt) => new(
        HeaderAr: string.IsNullOrWhiteSpace(titleAr) ? "إعلان جديد" : titleAr,
        HeaderEn: string.IsNullOrWhiteSpace(titleEn) ? "New announcement" : titleEn,
        ContentAr: Excerpt(excerpt),
        ContentEn: Excerpt(excerpt));

    /// <summary>
    /// Trims quoted user content to something that fits a notification row.
    ///
    /// Takes plain text, never HTML. The caller passes <c>Content_Text</c>, which the
    /// sanitiser produced - putting markup here would push unescaped content into an
    /// email body and into whatever the mobile client does with it.
    /// </summary>
    private static string Excerpt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var collapsed = text.Trim();

        return collapsed.Length <= ExcerptLength
            ? collapsed
            : string.Concat(collapsed.AsSpan(0, ExcerptLength).TrimEnd(), "\u2026");
    }

    private static string ReactionAr(ReactionType reaction) => reaction switch
    {
        ReactionType.Love => "أحبّه",
        ReactionType.Celebrate => "احتفل به",
        ReactionType.Support => "دعمه",
        ReactionType.Insightful => "وجده ملهمًا",
        _ => "أعجبه",
    };

    private static string ReactionEn(ReactionType reaction) => reaction switch
    {
        ReactionType.Love => "Loved it",
        ReactionType.Celebrate => "Celebrated it",
        ReactionType.Support => "Supported it",
        ReactionType.Insightful => "Found it insightful",
        _ => "Liked it",
    };

    /// <summary>One notification's wording, both languages, header and body.</summary>
    public readonly record struct Text(string HeaderAr, string HeaderEn, string ContentAr, string ContentEn);
}

/// <summary>
/// Where a notification takes you when it is clicked.
///
/// Client-relative paths, not absolute URLs. The API does not know the SPA's host, and
/// baking one in would break the moment the product is reached through a different
/// domain - which for an intranet product it always eventually is.
/// </summary>
public static class NotificationLinks
{
    public static string Post(Guid postId) => $"/feed/{postId:D}";

    public static string Comment(Guid postId, Guid commentId) => $"/feed/{postId:D}?comment={commentId:D}";

    public static string Employee(Guid employeeId) => $"/employees/{employeeId:D}";
}
