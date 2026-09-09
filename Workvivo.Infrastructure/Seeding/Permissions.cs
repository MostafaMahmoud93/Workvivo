namespace Workvivo.Infrastructure.Seeding;

/// <summary>
/// Every named permission in the system, as compile-time constants.
///
/// Constants rather than strings at the call site so that
/// <c>[HasPermission(Permissions.Post.Create)]</c> is checked by the compiler. A typo
/// in a literal would not fail - it would produce a permission nobody holds, and the
/// endpoint would quietly refuse everyone.
/// </summary>
public static class Permissions
{
    public static class Employee
    {
        public const string View = "Employee.View";
        public const string Create = "Employee.Create";
        public const string Edit = "Employee.Edit";
        public const string Delete = "Employee.Delete";
    }

    public static class Post
    {
        public const string View = "Post.View";
        public const string Create = "Post.Create";
        public const string Edit = "Post.Edit";
        public const string Delete = "Post.Delete";
        public const string Moderate = "Post.Moderate";
    }

    public static class Announcement
    {
        public const string Create = "Announcement.Create";
        public const string Publish = "Announcement.Publish";
    }

    public static class Community
    {
        public const string Manage = "Community.Manage";
    }

    public static class Survey
    {
        public const string Manage = "Survey.Manage";
    }

    public static class Poll
    {
        public const string Manage = "Poll.Manage";
    }

    public static class Event
    {
        public const string Manage = "Event.Manage";
    }

    public static class Recognition
    {
        public const string Manage = "Recognition.Manage";
    }

    public static class Document
    {
        public const string View = "Document.View";
        public const string Manage = "Document.Manage";
    }

    public static class Organization
    {
        public const string Manage = "Organization.Manage";
    }

    public static class Role
    {
        public const string Manage = "Role.Manage";
    }

    public static class Analytics
    {
        public const string View = "Analytics.View";
    }

    public static class Settings
    {
        public const string Manage = "Settings.Manage";
    }

    public static class AuditLog
    {
        public const string View = "AuditLog.View";
    }
}

/// <summary>
/// The seven seeded roles, by their normalised name.
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SUPERADMIN";
    public const string Admin = "ADMIN";
    public const string Hr = "HR";
    public const string CommunicationsManager = "COMMUNICATIONSMANAGER";
    public const string DepartmentManager = "DEPARTMENTMANAGER";
    public const string Moderator = "MODERATOR";
    public const string Employee = "EMPLOYEE";
}
