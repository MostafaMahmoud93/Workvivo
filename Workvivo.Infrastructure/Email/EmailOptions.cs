using System.ComponentModel.DataAnnotations;

namespace Workvivo.Infrastructure.Email;

/// <summary>
/// How outbound notification mail is sent.
///
/// Separate from the template's <c>MailSettings</c>, which carries an Exchange Web
/// Services branch and template ids this path does not use. Both read the same host and
/// credentials from configuration; neither holds a password in a committed file.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Off by default.
    ///
    /// A development machine with real SMTP credentials in its environment will
    /// otherwise mail seeded employees at their seeded addresses the first time
    /// somebody comments on a test post.
    /// </summary>
    public bool Enabled { get; set; }

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    /// <summary>STARTTLS on the submission port. Plaintext SMTP is never a valid choice here.</summary>
    public bool UseStartTls { get; set; } = true;

    public string? Username { get; set; }

    /// <summary>
    /// Never present in a committed file. Supplied as <c>Email__Password</c> from the
    /// environment or a secret store.
    /// </summary>
    public string? Password { get; set; }

    [EmailAddress]
    public string? FromAddress { get; set; }

    public string FromDisplayName { get; set; } = "Workvivo";

    /// <summary>
    /// Absolute base URL of the SPA, used to turn a notification's relative link into
    /// something clickable in an email client.
    /// </summary>
    public string? ClientBaseUrl { get; set; }
}
