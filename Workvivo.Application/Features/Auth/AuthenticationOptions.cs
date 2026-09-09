namespace Workvivo.Application.Features.Auth;

/// <summary>Sign-in policy, from the "Authentication" configuration section.</summary>
public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    /// <summary>Failed attempts before the account is locked.</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>How long a lockout lasts.</summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Window over which failed attempts are counted.</summary>
    public TimeSpan FailedAttemptWindow { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Whether an unverified email address blocks sign-in.</summary>
    public bool RequireConfirmedEmail { get; set; }

    /// <summary>How long a password-reset link stays valid.</summary>
    public TimeSpan PasswordResetLifetime { get; set; } = TimeSpan.FromHours(2);
}
