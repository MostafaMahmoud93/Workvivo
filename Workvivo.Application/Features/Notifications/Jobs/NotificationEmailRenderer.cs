using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Workvivo.Domain.Models.Email;
using Workvivo.Infrastructure.Email;

namespace Workvivo.Application.Features.Notifications.Jobs;

/// <summary>
/// Turns notification rows into an email.
///
/// Hand-built rather than templated. The markup an email client will render is a small,
/// old-fashioned subset - tables and inline styles, no flexbox, no external stylesheet -
/// and a template engine adds a dependency and a file to keep in sync without changing
/// what has to be written.
///
/// Everything interpolated is HTML-encoded here. The notification body already went
/// through the sanitiser as plain text, but encoding at the point of rendering is what
/// makes that a belt as well as braces: a future caller that forgets cannot turn a
/// colleague's post into markup inside somebody's inbox.
/// </summary>
public sealed class NotificationEmailRenderer
{
    private readonly EmailOptions _options;

    public NotificationEmailRenderer(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>One notification, sent on its own.</summary>
    public EmailMessage Single(string recipientEmail, string header, string content, string? relativeLink, string? correlationId)
    {
        var body = new StringBuilder();

        body.Append(Open());
        body.Append(Heading(header));

        if (!string.IsNullOrWhiteSpace(content))
        {
            body.Append(Paragraph(content));
        }

        body.Append(Button(relativeLink, "Open in Workvivo"));
        body.Append(Close());

        return new EmailMessage
        {
            To = [recipientEmail],
            Subject = header,
            HtmlBody = body.ToString(),
            TextBody = Text(header, content, relativeLink),
            CorrelationId = correlationId,
        };
    }

    /// <summary>Several notifications, batched into one message.</summary>
    public EmailMessage Digest(
        string recipientEmail,
        string subject,
        IReadOnlyList<(string Header, string Content, string? Link)> items,
        string? correlationId)
    {
        var body = new StringBuilder();
        var text = new StringBuilder();

        body.Append(Open());
        body.Append(Heading(subject));

        text.AppendLine(subject).AppendLine();

        foreach (var (header, content, link) in items)
        {
            body.Append("<tr><td style=\"padding:12px 0;border-bottom:1px solid #e6e8eb;\">");
            body.Append($"<div style=\"font-weight:600;color:#111827;\">{Encode(header)}</div>");

            if (!string.IsNullOrWhiteSpace(content))
            {
                body.Append($"<div style=\"color:#4b5563;margin-top:4px;\">{Encode(content)}</div>");
            }

            if (Absolute(link) is { } url)
            {
                body.Append($"<a href=\"{Encode(url)}\" style=\"color:#2563eb;\">Open</a>");
            }

            body.Append("</td></tr>");

            text.AppendLine($"- {header}");

            if (!string.IsNullOrWhiteSpace(content))
            {
                text.AppendLine($"  {content}");
            }
        }

        body.Append(Close());

        return new EmailMessage
        {
            To = [recipientEmail],
            Subject = subject,
            HtmlBody = body.ToString(),
            TextBody = text.ToString(),
            CorrelationId = correlationId,
        };
    }

    private static string Open() =>
        "<div style=\"font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;background:#f7f8fa;padding:24px;\">"
        + "<table role=\"presentation\" width=\"100%\" style=\"max-width:560px;margin:0 auto;background:#ffffff;"
        + "border-radius:12px;padding:24px;\"><tbody>";

    private static string Close() => "</tbody></table></div>";

    private static string Heading(string header) =>
        $"<tr><td style=\"font-size:18px;font-weight:600;color:#111827;padding-bottom:8px;\">{Encode(header)}</td></tr>";

    private static string Paragraph(string content) =>
        $"<tr><td style=\"color:#4b5563;line-height:1.5;padding-bottom:16px;\">{Encode(content)}</td></tr>";

    private string Button(string? relativeLink, string label)
    {
        if (Absolute(relativeLink) is not { } url)
        {
            return string.Empty;
        }

        return $"<tr><td><a href=\"{Encode(url)}\" style=\"display:inline-block;background:#2563eb;color:#ffffff;"
            + $"text-decoration:none;padding:10px 18px;border-radius:8px;\">{Encode(label)}</a></td></tr>";
    }

    private string Text(string header, string content, string? relativeLink)
    {
        var text = new StringBuilder().AppendLine(header);

        if (!string.IsNullOrWhiteSpace(content))
        {
            text.AppendLine().AppendLine(content);
        }

        if (Absolute(relativeLink) is { } url)
        {
            text.AppendLine().AppendLine(url);
        }

        return text.ToString();
    }

    /// <summary>
    /// Makes a stored relative link clickable.
    ///
    /// Notifications store client-relative paths because the API does not know the
    /// SPA's host. An email has to carry an absolute URL, so the base comes from
    /// configuration - and when it is not configured the link is omitted rather than
    /// rendered broken.
    /// </summary>
    private string? Absolute(string? relativeLink)
    {
        if (string.IsNullOrWhiteSpace(relativeLink) || string.IsNullOrWhiteSpace(_options.ClientBaseUrl))
        {
            return null;
        }

        return $"{_options.ClientBaseUrl.TrimEnd('/')}/{relativeLink.TrimStart('/')}";
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
