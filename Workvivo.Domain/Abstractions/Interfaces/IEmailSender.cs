using Workvivo.Domain.Models.Email;

namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Sends one already-composed message.
///
/// Separate from the template's <c>IMailService</c>, which is a different job: that
/// one renders a numbered template from the database, records a history row and
/// accepts uploaded attachments. This is the transport underneath, with no opinion
/// about where the body came from, so notification email, digests and - later -
/// password reset can share one implementation and one set of credentials.
///
/// Implementations must not throw for an undeliverable address. A single bad mailbox
/// in a digest of four hundred people must not fail the job and have Hangfire retry
/// the whole batch, sending the other three hundred and ninety nine again.
/// </summary>
public interface IEmailSender
{
    /// <summary>True when the message was handed to the mail server.</summary>
    Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
