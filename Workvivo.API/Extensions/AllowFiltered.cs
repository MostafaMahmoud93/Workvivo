namespace Workvivo.API.Extensions;
public static class AllowFiltered
{
    public static List<string> Controllers { get; set; } = new List<string> {
       "SysSetting",
       "MasterData",
       "Screen"
    };
    public static List<string> Actions { get; set; } = new List<string> {
       "UpdateImgeSignatureUser",
       "EditUserProfilePicture",
       "GetScreenActionsDDL",
       "GetMainModulesDDL",
       "GetCurrentUser",
       "GetScreensDDL",
       "GetGroupsDDL",
       "GetUsersDDL",
       "SearchUser"
    };
}