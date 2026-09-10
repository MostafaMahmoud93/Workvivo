namespace Workvivo.Domain.Models.Email;

/// <summary>
/// One outbound message, as the notification pipeline sees it.
///
/// Deliberately not <see cref="MailRequestModel"/>: that type carries
/// <c>IFormFile</c> attachments, which belong to an HTTP request and cannot be
/// serialised into a background job. This one is plain data, so a Hangfire worker can
/// rehydrate it minutes after the request that caused it has gone.
/// </summary>
public sealed class EmailMessage
{
    public required IReadOnlyList<string> To { get; init; }

    public required string Subject { get; init; }

    /// <summary>HTML body. Already built and sanitised by the caller.</summary>
    public required string HtmlBody { get; init; }

    /// <summary>
    /// Plain-text alternative. Not optional in practice: a message with no text part
    /// scores badly with spam filters and is unreadable in text-only clients.
    /// </summary>
    public required string TextBody { get; init; }

    /// <summary>Correlates the send with the request or job that caused it.</summary>
    public string? CorrelationId { get; init; }
}
