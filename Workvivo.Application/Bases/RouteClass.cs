namespace Workvivo.Application.Bases;
public static class RouteClass
{
    /// <summary>
    /// Ex. api[key word]/Auth[Controller Name]/Login[Action Name]
    /// </summary>
    /// <summary>
    /// Auth routes are lowercase, and must stay that way.
    ///
    /// The refresh cookie is scoped with Path=/api/auth so the long-lived credential is
    /// not attached to every request. Cookie path matching is case-sensitive (RFC 6265),
    /// in curl and in every browser - so a request to /api/Auth/Refresh does not carry a
    /// cookie set for /api/auth, and refresh silently fails with a 401 that looks like an
    /// expired session. ASP.NET routing is case-insensitive and hides this server-side.
    /// </summary>
    public static class Auth
    {
        public const string Login = "api/auth/login";
        public const string Refresh = "api/auth/refresh";
        public const string Logout = "api/auth/logout";
        public const string LogoutAll = "api/auth/logout-all";
        public const string Me = "api/auth/me";
        public const string ChangePassword = "api/auth/change-password";
        public const string Test = "api/auth/test";
    }
    public static class User
    {
        public const string GetSmartApprovalUsersDDL = "api/User/GetSmartApprovalUsersDDL";
        public const string UpdateImgeSignatureUser = "api/User/UpdateImgeSignatureUser";
        public const string EditUserProfilePicture = "api/User/EditUserProfilePicture";
        public const string UpdateImgeProfileUser = "api/User/UpdateImgeProfileUser";
        public const string GetCurrentUser = "api/User/GetCurrentUser";
        public const string DeleteUser = "api/User/DeleteUser";
        public const string CreateUser = "api/User/CreateUser";
        public const string SearchUser = "api/User/SearchUser";
        public const string EditUser = "api/User/EditUser";
        public const string GetUsers = "api/User/GetUsers";
        public const string GetUser = "api/User/GetUser";
    }
    public static class Group
    {
        public const string GetGroupsDDL = "api/Group/GetGroupsDDL";
        public const string DeleteGroup = "api/Group/DeleteGroup";
        public const string CreateGroup = "api/Group/CreateGroup";
        public const string EditGroup = "api/Group/EditGroup";
        public const string GetGroups = "api/Group/GetGroups";
    }
    public static class Employees
    {
        public const string Directory = "api/employees";
        public const string Suggest = "api/employees/suggest";
        public const string Me = "api/employees/me";
        public const string UpdateMe = "api/employees/me";
        public const string Profile = "api/employees/{employeeId:guid}";
        public const string Follow = "api/employees/{employeeId:guid}/follow";
        public const string Unfollow = "api/employees/{employeeId:guid}/follow";
    }

    public static class Posts
    {
        public const string Feed = "api/posts/feed";
        public const string Create = "api/posts";
        public const string React = "api/posts/{postId:guid}/reactions";
        public const string State = "api/posts/{postId:guid}/state";
        public const string Views = "api/posts/views";
    }

    public static class Comments
    {
        public const string List = "api/posts/{postId:guid}/comments";
        public const string Add = "api/posts/{postId:guid}/comments";
        public const string Delete = "api/comments/{commentId:guid}";
    }

    /// <summary>
    /// Everything here is scoped to the caller by the handler, never by a route
    /// parameter. There is deliberately no "notifications for employee {id}" endpoint.
    /// </summary>
    public static class Notifications
    {
        public const string List = "api/notifications";
        public const string UnreadCount = "api/notifications/unread-count";
        public const string MarkRead = "api/notifications/read";
        public const string Preferences = "api/notifications/preferences";
    }

    public static class Communities
    {
        public const string List = "api/communities";
        public const string Save = "api/communities";
        public const string Detail = "api/communities/{communityId:guid}";
        public const string Members = "api/communities/{communityId:guid}/members";
        public const string Join = "api/communities/{communityId:guid}/membership";
        public const string Leave = "api/communities/{communityId:guid}/membership";
        public const string Review = "api/communities/{communityId:guid}/members/{employeeId:guid}/review";
        public const string Role = "api/communities/{communityId:guid}/members/{employeeId:guid}/role";
        public const string Invite = "api/communities/{communityId:guid}/invitations";
        public const string MyInvitations = "api/communities/invitations/mine";
        public const string RespondToInvitation = "api/communities/invitations/{invitationId:guid}";
    }

    public static class RecognitionRoutes
    {
        public const string Types = "api/recognition/types";
        public const string Wall = "api/recognition";
        public const string Give = "api/recognition";
        public const string Leaderboard = "api/recognition/leaderboard";
    }

    public static class Polls
    {
        public const string List = "api/polls";
        public const string Save = "api/polls";
        public const string Vote = "api/polls/{pollId:guid}/votes";
    }

    public static class Surveys
    {
        public const string List = "api/surveys";
        public const string Detail = "api/surveys/{surveyId:guid}";
        public const string Respond = "api/surveys/{surveyId:guid}/responses";
        public const string Results = "api/surveys/{surveyId:guid}/results";
    }

    public static class Events
    {
        public const string List = "api/events";
        public const string Save = "api/events";
        public const string Rsvp = "api/events/{eventId:guid}/rsvp";
    }

    public static class Documents
    {
        public const string List = "api/documents";
        public const string Categories = "api/documents/categories";
        public const string Upload = "api/documents";
        public const string Download = "api/documents/{documentId:guid}/content";
    }

    public static class SearchRoutes
    {
        public const string Search = "api/search";
    }

    public static class AnalyticsRoutes
    {
        public const string Dashboard = "api/analytics/dashboard";
    }

    public static class Admin
    {
        public const string Roles = "api/admin/roles";
        public const string AuditLog = "api/admin/audit";
    }

    public static class OrganizationRoutes
    {
        public const string DepartmentTree = "api/organization/departments/tree";
        public const string Lookups = "api/organization/lookups";
        public const string SaveDepartment = "api/organization/departments";
        public const string DeleteDepartment = "api/organization/departments/{id:guid}";
    }

    public static class Screen
    {
        public const string GetScreens = "api/Screen/GetScreens";
    }
    public static class UsersShortCuts
    {
        public const string DeleteUsersShortCuts = "api/UsersShortCuts/DeleteUsersShortCuts";
        public const string SaveUsersShortCuts = "api/UsersShortCuts/SaveUsersShortCuts";
        public const string GetUsersShortCuts = "api/UsersShortCuts/GetUsersShortCuts";
    }
    public static class EmailSMSTemplates
    {
        public const string GetDefultEmailSMSTemplateById = "api/EmailSMSTemplates/GetDefultEmailSMSTemplateById";
        public const string GetEmailSMSTemplateById = "api/EmailSMSTemplates/GetEmailSMSTemplateById";
        public const string GetEmailSMSTemplates = "api/EmailSMSTemplates/GetEmailSMSTemplates";
        public const string EditEmailSMSTemplate = "api/EmailSMSTemplates/EditEmailSMSTemplate";
    }
    public static class MasterData
    {
        public const string GetMasterDataByCode = "api/MasterData/GetMasterDataByCode";
        public const string GetUserType = "api/MasterData/GetUserType";
        public const string GetIcons = "api/MasterData/GetIcons";
    }
    public static class GroupAction
    {
        public const string GetGroupActions = "api/GroupAction/GetGroupActions/{groupId}";
        public const string GetUserActions = "api/GroupAction/GetUserActions/{userId}";
        public const string AddEditGroupAction = "api/GroupAction/AddEditGroupAction";
        public const string AddEditUserAction = "api/GroupAction/AddEditUserAction";
    }
    public static class SysSetting
    {
        public const string UpdateSystemStamp = "api/SysSetting/UpdateSystemStamp";
        public const string GetSystemStamp = "api/SysSetting/GetSystemStamp";
    }
    public static class HistoryReport
    {
        public const string GetScreenActionsDDL = "api/HistoryReport/GetScreenActionsDDL";
        public const string GetMainModulesDDL = "api/HistoryReport/GetMainModulesDDL";
        public const string GetHistoryReport = "api/HistoryReport/GetHistoryReport";
        public const string GetScreensDDL = "api/HistoryReport/GetScreensDDL";
        public const string GetUsersDDL = "api/HistoryReport/GetUsersDDL";
    }   
}