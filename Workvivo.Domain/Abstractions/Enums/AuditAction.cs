namespace Workvivo.Domain.Abstractions.Enums;

/// <summary>
/// Operations worth an audit row.
///
/// Deliberately not one value per endpoint: the audit log answers "who changed what,
/// and when", so the vocabulary is the shape of the change, with the entity name and
/// id carrying the specifics.
/// </summary>
public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Login = 3,
    LoginFailed = 4,
    Logout = 5,
    Publish = 6,
    Unpublish = 7,
    PermissionChange = 8,
    RoleChange = 9,
    Activate = 10,
    Deactivate = 11,
    PasswordReset = 12,
    Download = 13,
    Upload = 14,
    Export = 15,
    Moderate = 16,
    SettingsChange = 17,
    TokenReuseDetected = 18,
}
