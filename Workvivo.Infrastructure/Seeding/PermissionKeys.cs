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
}
