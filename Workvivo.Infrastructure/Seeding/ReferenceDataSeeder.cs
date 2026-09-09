using Workvivo.Infrastructure.Abstractions;

namespace Workvivo.Infrastructure.Seeding;

/// <summary>
/// Reference data that is part of the schema contract: roles, the permission
/// catalogue, recognition categories and document categories.
///
/// This is deliberately not demo data. Everything here is something the application
/// depends on to function - a missing permission row means an endpoint nobody can
/// call - so it ships in the migration and is present in every environment.
///
/// Sample employees, posts and communities are a different thing entirely and belong
/// to a development-only runtime seeder, so that production never gets them and a
/// migration is not the place where fake people are defined.
/// </summary>
internal static class ReferenceDataSeeder
{
    /// <summary>The account the template already seeds, used as the author of seeded rows.</summary>
    private static readonly Guid SystemUserId = new("8f6c1f5a-3c9e-4c2b-9d17-2a5b4e7c1d90");

    /// <summary>Fixed timestamp - DateTime.UtcNow here would make the model non-deterministic.</summary>
    private static readonly DateTime SeededOn = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>The Management module the template seeds.</summary>
    private static readonly Guid ManagementModuleId = new("986b68ac-22ff-4cee-991d-dbdfe4b12bbb");

    /// <summary>Screen actions the template already seeds, reused rather than duplicated.</summary>
    private static class Actions
    {
        public static readonly Guid View = new("6EBD6AE4-7F43-45B6-9D57-AA5067A4B79F");
        public static readonly Guid Add = new("F820B435-D621-41A8-80E4-32D5D1F867C7");
        public static readonly Guid Edit = new("4ADD3D50-F32C-4ABE-B8E2-3AF596588A71");
        public static readonly Guid Delete = new("328723DA-E3C9-468E-998D-6ADEA7217D37");
        public static readonly Guid Approve = new("8A231277-B772-4C59-844E-54435DC912F1");
    }

    public static void Seed(ModelBuilder builder)
    {
        SeedRoles(builder);
        SeedScreens(builder);
        SeedPermissions(builder);
        SeedRolePermissions(builder);
        SeedRecognitionTypes(builder);
        SeedDocumentCategories(builder);
    }

    #region Roles

    private static readonly (string Normalized, string NameAr, string NameEn)[] RoleDefinitions =
    [
        (Roles.SuperAdmin, "مدير النظام", "Super Admin"),
        (Roles.Admin, "مسؤول", "Admin"),
        (Roles.Hr, "الموارد البشرية", "HR"),
        (Roles.CommunicationsManager, "مدير الاتصال", "Communications Manager"),
        (Roles.DepartmentManager, "مدير إدارة", "Department Manager"),
        (Roles.Moderator, "مشرف المحتوى", "Moderator"),
        (Roles.Employee, "موظف", "Employee"),
    ];

    private static Guid RoleId(string normalized) => DeterministicGuid.From($"role:{normalized}");

    private static void SeedRoles(ModelBuilder builder)
    {
        var roles = RoleDefinitions.Select(r => new UserGroup
        {
            Id = RoleId(r.Normalized),
            Name = r.NameEn,
            NormalizedName = r.Normalized,
            Name_Ar = r.NameAr,
            Name_En = r.NameEn,
            User_Type = "MANAG",
            Is_Active = true,
            Is_Deleted = false,
            Created_By = SystemUserId,
            Create_Date = SeededOn,

            // IdentityRole initialises this with Guid.NewGuid(); pinning it is what
            // keeps the model deterministic. The template's original seeded role was
            // missing this and made "ef database update" refuse to run.
            ConcurrencyStamp = DeterministicGuid.From($"role-stamp:{r.Normalized}").ToString(),
        }).ToArray();

        builder.Entity<UserGroup>().HasData(roles);
    }

    #endregion

    #region Screens

    /// <summary>
    /// A permission has to hang off a screen, because the same rows drive the
    /// navigation menu. These are the platform's own screens, added alongside the
    /// template's existing security ones.
    /// </summary>
    private static readonly (string Key, string DescAr, string DescEn, string Link, string Icon, int Order)[] ScreenDefinitions =
    [
        ("feed", "الأخبار", "Feed", "feed", "heroicons_outline:newspaper", 10),
        ("employees", "الموظفون", "Employees", "employees", "heroicons_outline:users", 20),
        ("organization", "الهيكل التنظيمي", "Organization", "admin/organization", "heroicons_outline:building-office", 30),
        ("communities", "المجتمعات", "Communities", "communities", "heroicons_outline:user-group", 40),
        ("recognition", "التقدير", "Recognition", "recognition", "heroicons_outline:trophy", 50),
        ("surveys", "الاستبيانات", "Surveys", "surveys", "heroicons_outline:clipboard-document-check", 60),
        ("events", "الفعاليات", "Events", "events", "heroicons_outline:calendar-days", 70),
        ("documents", "المستندات", "Documents", "documents", "heroicons_outline:document-text", 80),
        ("analytics", "التحليلات", "Analytics", "admin/analytics", "heroicons_outline:chart-bar", 90),
        ("audit", "سجل التدقيق", "Audit Log", "admin/audit", "heroicons_outline:shield-check", 100),
        ("settings", "الإعدادات", "Settings", "admin/settings", "heroicons_outline:cog-6-tooth", 110),
    ];

    private static Guid ScreenId(string key) => DeterministicGuid.From($"screen:{key}");

    private static void SeedScreens(ModelBuilder builder)
    {
        var screens = ScreenDefinitions.Select(s => new Screen
        {
            Id = ScreenId(s.Key),
            Main_Module_Id = ManagementModuleId,
            Screen_Description_Ar = s.DescAr,
            Screen_Description_En = s.DescEn,
            Parent_Screen_Id = null,
            Link = s.Link,
            Is_Branch = false,
            Order = s.Order,
            Menu_Or_Not = true,
            No_Login = true,
            Screen_Icon = s.Icon,
            Is_Deleted = false,
        }).ToArray();

        builder.Entity<Screen>().HasData(screens);
    }

    #endregion

    #region Permissions

    /// <summary>
    /// The permission catalogue: one row per named permission, each tied to the screen
    /// it belongs to and the action it represents.
    ///
    /// Base_Route is left empty for these. The template's legacy ActionFilter matches
    /// on route prefixes; the platform's own checks match on Permission_Key, and the
    /// authorisation phase replaces that filter outright.
    /// </summary>
    internal static readonly PermissionDefinition[] PermissionDefinitions =
    [
        new(Permissions.Employee.View, "employees", Actions.View, "Employees", "عرض الموظفين", "View employees"),
        new(Permissions.Employee.Create, "employees", Actions.Add, "Employees", "إضافة موظف", "Create employees"),
        new(Permissions.Employee.Edit, "employees", Actions.Edit, "Employees", "تعديل موظف", "Edit employees"),
        new(Permissions.Employee.Delete, "employees", Actions.Delete, "Employees", "حذف موظف", "Delete employees"),

        new(Permissions.Post.View, "feed", Actions.View, "Feed", "عرض المنشورات", "View posts"),
        new(Permissions.Post.Create, "feed", Actions.Add, "Feed", "إنشاء منشور", "Create posts"),
        new(Permissions.Post.Edit, "feed", Actions.Edit, "Feed", "تعديل منشور", "Edit posts"),
        new(Permissions.Post.Delete, "feed", Actions.Delete, "Feed", "حذف منشور", "Delete posts"),
        new(Permissions.Post.Moderate, "feed", Actions.Approve, "Feed", "إدارة المحتوى", "Moderate posts"),

        new(Permissions.Announcement.Create, "feed", Actions.Add, "Feed", "إنشاء إعلان", "Create announcements"),
        new(Permissions.Announcement.Publish, "feed", Actions.Approve, "Feed", "نشر إعلان", "Publish announcements"),

        new(Permissions.Community.Manage, "communities", Actions.Edit, "Communities", "إدارة المجتمعات", "Manage communities"),
        new(Permissions.Survey.Manage, "surveys", Actions.Edit, "Surveys", "إدارة الاستبيانات", "Manage surveys"),
        new(Permissions.Poll.Manage, "surveys", Actions.Add, "Surveys", "إدارة الاستطلاعات", "Manage polls"),
        new(Permissions.Event.Manage, "events", Actions.Edit, "Events", "إدارة الفعاليات", "Manage events"),
        new(Permissions.Recognition.Manage, "recognition", Actions.Edit, "Recognition", "إدارة التقدير", "Manage recognition"),

        new(Permissions.Document.View, "documents", Actions.View, "Documents", "عرض المستندات", "View documents"),
        new(Permissions.Document.Manage, "documents", Actions.Edit, "Documents", "إدارة المستندات", "Manage documents"),

        new(Permissions.Organization.Manage, "organization", Actions.Edit, "Organization", "إدارة الهيكل التنظيمي", "Manage the organisation"),
        new(Permissions.Role.Manage, "settings", Actions.Edit, "Security", "إدارة الأدوار والصلاحيات", "Manage roles and permissions"),
        new(Permissions.Analytics.View, "analytics", Actions.View, "Analytics", "عرض التحليلات", "View analytics"),
        new(Permissions.Settings.Manage, "settings", Actions.Edit, "Settings", "إدارة الإعدادات", "Manage settings"),
        new(Permissions.AuditLog.View, "audit", Actions.View, "Security", "عرض سجل التدقيق", "View the audit log"),
    ];

    private static Guid PermissionId(string key) => DeterministicGuid.From($"perm:{key}");

    private static void SeedPermissions(ModelBuilder builder)
    {
        var permissions = PermissionDefinitions.Select(p => new LinkScreenAction
        {
            Id = PermissionId(p.Key),
            Screen_Id = ScreenId(p.Screen),
            Screen_Action_Id = p.Action,
            Base_Route = string.Empty,

            // The legacy Action_Code column is a five-character code. Derived from the
            // permission key so it stays unique and stable without inventing a second
            // naming scheme to maintain by hand.
            Action_Code = BuildLegacyActionCode(p.Key),
            Permission_Key = p.Key,
            Module = p.Module,
            Description_Ar = p.DescAr,
            Description_En = p.DescEn,
            Is_Deleted = false,
        }).ToArray();

        builder.Entity<LinkScreenAction>().HasData(permissions);
    }

    private static string BuildLegacyActionCode(string permissionKey)
    {
        var hash = DeterministicGuid.From($"code:{permissionKey}").ToString("N");
        return hash[..5].ToUpperInvariant();
    }

    #endregion

    #region Role permissions

    /// <summary>
    /// Which role holds which permission.
    ///
    /// Least privilege by default: a plain Employee can read and post, and nothing
    /// else. Everything administrative is granted deliberately, one permission at a
    /// time, rather than by giving a role a blanket flag.
    /// </summary>
    internal static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        // SuperAdmin is granted every permission explicitly rather than bypassing the
        // check, so the permission screen shows the truth and every grant is auditable.
        [Roles.SuperAdmin] = PermissionDefinitions.Select(p => p.Key).ToArray(),

        [Roles.Admin] =
        [
            Permissions.Employee.View, Permissions.Employee.Create, Permissions.Employee.Edit,
            Permissions.Post.View, Permissions.Post.Create, Permissions.Post.Edit,
            Permissions.Post.Delete, Permissions.Post.Moderate,
            Permissions.Announcement.Create, Permissions.Announcement.Publish,
            Permissions.Community.Manage, Permissions.Survey.Manage, Permissions.Poll.Manage,
            Permissions.Event.Manage, Permissions.Recognition.Manage,
            Permissions.Document.View, Permissions.Document.Manage,
            Permissions.Organization.Manage, Permissions.Analytics.View,
            Permissions.Settings.Manage, Permissions.AuditLog.View,
        ],

        [Roles.Hr] =
        [
            Permissions.Employee.View, Permissions.Employee.Create, Permissions.Employee.Edit,
            Permissions.Post.View, Permissions.Post.Create,
            Permissions.Announcement.Create, Permissions.Announcement.Publish,
            Permissions.Survey.Manage, Permissions.Poll.Manage,
            Permissions.Event.Manage, Permissions.Recognition.Manage,
            Permissions.Document.View, Permissions.Document.Manage,
            Permissions.Organization.Manage, Permissions.Analytics.View,
        ],

        [Roles.CommunicationsManager] =
        [
            Permissions.Employee.View,
            Permissions.Post.View, Permissions.Post.Create, Permissions.Post.Edit,
            Permissions.Post.Moderate,
            Permissions.Announcement.Create, Permissions.Announcement.Publish,
            Permissions.Community.Manage, Permissions.Event.Manage,
            Permissions.Poll.Manage, Permissions.Document.View,
            Permissions.Analytics.View,
        ],

        [Roles.DepartmentManager] =
        [
            Permissions.Employee.View,
            Permissions.Post.View, Permissions.Post.Create,
            Permissions.Recognition.Manage, Permissions.Event.Manage,
            Permissions.Document.View, Permissions.Analytics.View,
        ],

        [Roles.Moderator] =
        [
            Permissions.Employee.View,
            Permissions.Post.View, Permissions.Post.Create, Permissions.Post.Moderate,
            Permissions.Post.Delete, Permissions.Community.Manage,
            Permissions.Document.View,
        ],

        [Roles.Employee] =
        [
            Permissions.Employee.View,
            Permissions.Post.View, Permissions.Post.Create,
            Permissions.Document.View,
        ],
    };

    private static void SeedRolePermissions(ModelBuilder builder)
    {
        var grants = RolePermissions
            .SelectMany(pair => pair.Value.Select(permissionKey => new GroupPermissions
            {
                Id = DeterministicGuid.From($"grant:{pair.Key}:{permissionKey}"),
                Group_Id = RoleId(pair.Key),
                Link_Screen_Action_Id = PermissionId(permissionKey),
                Is_Deleted = false,
            }))
            .ToArray();

        builder.Entity<GroupPermissions>().HasData(grants);
    }

    #endregion

    #region Recognition types

    private static readonly (string Code, string NameAr, string NameEn, string Icon, string Color, int Points, int Order)[] RecognitionTypeDefinitions =
    [
        ("PERF", "أداء متميز", "Outstanding Performance", "trophy", "#c8892a", 50, 1),
        ("TEAM", "روح الفريق", "Teamwork", "users", "#2a7ac8", 30, 2),
        ("INNO", "الابتكار", "Innovation", "light-bulb", "#7a2ac8", 40, 3),
        ("CUST", "تميز خدمة العملاء", "Customer Excellence", "heart", "#c82a5a", 40, 4),
        ("LEAD", "القيادة", "Leadership", "flag", "#1f4e79", 50, 5),
        ("ABOV", "تجاوز التوقعات", "Going Above and Beyond", "rocket-launch", "#2ac88a", 60, 6),
    ];

    private static void SeedRecognitionTypes(ModelBuilder builder)
    {
        var types = RecognitionTypeDefinitions.Select(t => new RecognitionType
        {
            Id = DeterministicGuid.From($"rectype:{t.Code}"),
            Code = t.Code,
            Name_Ar = t.NameAr,
            Name_En = t.NameEn,
            Badge_Icon = t.Icon,
            Badge_Color = t.Color,
            Default_Points = t.Points,
            Sort_Order = t.Order,
            Is_Active = true,
            Is_Deleted = false,
            Created_By = SystemUserId,
            Create_Date = SeededOn,
        }).ToArray();

        builder.Entity<RecognitionType>().HasData(types);
    }

    #endregion

    #region Document categories

    private static readonly (string Key, string NameAr, string NameEn, string Icon, int Order)[] DocumentCategoryDefinitions =
    [
        ("hr-policies", "سياسات الموارد البشرية", "HR Policies", "identification", 1),
        ("it-policies", "سياسات تقنية المعلومات", "IT Policies", "computer-desktop", 2),
        ("forms", "النماذج", "Forms", "document-duplicate", 3),
        ("guidelines", "الإرشادات", "Guidelines", "book-open", 4),
        ("training", "التدريب", "Training", "academic-cap", 5),
        ("company", "مستندات الشركة", "Company Documents", "building-office", 6),
    ];

    private static void SeedDocumentCategories(ModelBuilder builder)
    {
        var categories = DocumentCategoryDefinitions.Select(c => new DocumentCategory
        {
            Id = DeterministicGuid.From($"doccat:{c.Key}"),
            Parent_Category_Id = null,
            Name_Ar = c.NameAr,
            Name_En = c.NameEn,
            Icon = c.Icon,
            Sort_Order = c.Order,
            Is_Active = true,
            Is_Deleted = false,
            Created_By = SystemUserId,
            Create_Date = SeededOn,
        }).ToArray();

        builder.Entity<DocumentCategory>().HasData(categories);
    }

    #endregion
}
