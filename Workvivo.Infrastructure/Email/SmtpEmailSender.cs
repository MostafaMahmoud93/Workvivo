using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Models.Email;

namespace Workvivo.Infrastructure.Email;

/// <summary>
/// Sends over SMTP with MailKit.
///
/// One connection per message. A pooled connection would be faster, but SMTP servers
/// drop idle sessions and a stale pooled connection fails on the send rather than on
/// the connect - which is a much harder failure to diagnose from a queue worker.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (message.To.Count == 0)
        {
            return false;
        }

        try
        {
            var mime = Build(message);

            using var client = new SmtpClient();

            // SecureSocketOptions.StartTls, not Auto. Auto falls back to an unencrypted
            // session when the server does not advertise STARTTLS, which would put
            // credentials and message bodies on the wire in clear text.
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            // Swallowed on purpose - see IEmailSender. One undeliverable mailbox inside
            // a four-hundred-person digest must not fail the job and have it retried
            // against everybody else.
            _logger.LogError(
                ex,
                "Sending {Subject} to {RecipientCount} recipient(s) failed",
                message.Subject,
                message.To.Count);

            return false;
        }
    }

    private MimeMessage Build(EmailMessage message)
    {
        var mime = new MimeMessage();

        mime.From.Add(new MailboxAddress(_options.FromDisplayName, _options.FromAddress));

        foreach (var recipient in message.To)
        {
            mime.To.Add(MailboxAddress.Parse(recipient));
        }

        mime.Subject = message.Subject;

        // Both parts. A message with no plain-text alternative scores badly with spam
        // filters and is unreadable in a text-only client.
        mime.Body = new MultipartAlternative
        {
            new TextPart(TextFormat.Plain) { Text = message.TextBody },
            new TextPart(TextFormat.Html) { Text = message.HtmlBody },
        };

        if (!string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            mime.Headers.Add("X-Correlation-Id", message.CorrelationId);
        }

        return mime;
    }
}

/// <summary>
/// Writes the message to the log instead of sending it.
///
/// What runs on a development machine and in tests. Deliberately logs the subject and
/// the recipients but not the body: a digest body quotes colleagues' posts, and putting
/// that in a log file turns the log into a copy of the feed.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email is disabled. Would have sent \"{Subject}\" to {Recipients}",
            message.Subject,
            string.Join(", ", message.To));

        return Task.FromResult(true);
    }
}
