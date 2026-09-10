namespace Workvivo.Infrastructure.Seeding;

/// <summary>
/// Permission keys as plain constants, usable from the Application layer.
///
/// <see cref="Permissions"/> groups them into nested classes for readability at call
/// sites in the API; this flat set exists so a handler can reference one without
/// pulling the whole nested hierarchy into scope.
/// </summary>
public static class PermissionKeys
{
    public const string PostView = Permissions.Post.View;
    public const string PostCreate = Permissions.Post.Create;
    public const string PostEdit = Permissions.Post.Edit;
    public const string PostDelete = Permissions.Post.Delete;
    public const string PostModerate = Permissions.Post.Moderate;
    public const string AnnouncementCreate = Permissions.Announcement.Create;
    public const string AnnouncementPublish = Permissions.Announcement.Publish;

    public const string EmployeeView = Permissions.Employee.View;
    public const string EmployeeEdit = Permissions.Employee.Edit;

    public const string CommunityManage = Permissions.Community.Manage;
    public const string RecognitionManage = Permissions.Recognition.Manage;
    public const string PollManage = Permissions.Poll.Manage;
    public const string SurveyManage = Permissions.Survey.Manage;
    public const string EventManage = Permissions.Event.Manage;
    public const string DocumentView = Permissions.Document.View;
    public const string DocumentManage = Permissions.Document.Manage;
    public const string OrganizationManage = Permissions.Organization.Manage;
    public const string RoleManage = Permissions.Role.Manage;
    public const string AnalyticsView = Permissions.Analytics.View;
    public const string SettingsManage = Permissions.Settings.Manage;
    public const string AuditLogView = Permissions.AuditLog.View;
}
