namespace Workvivo.Application.Bases;
public static class RouteClass
{
    /// <summary>
    /// Ex. api[key word]/Auth[Controller Name]/Login[Action Name]
    /// </summary>
    public static class Auth
    {
        public const string Login = "api/Auth/Login";
        public const string Test = "api/Auth/Test";
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