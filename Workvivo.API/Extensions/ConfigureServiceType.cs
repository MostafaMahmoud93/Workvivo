namespace Workvivo.API.Extensions;
public static class ConfigureServiceType
{
    public static void AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped(typeof(IApplicationUserService), typeof(ApplicationUserService));
        services.AddScoped(typeof(IGroupActionService), typeof(GroupActionService));
        services.AddScoped(typeof(IMasterDataService), typeof(MasterDataService));
        services.AddScoped(typeof(ISysSettingService), typeof(SysSettingService));
        services.AddScoped(typeof(IScreenService), typeof(ScreenService));
        services.AddScoped(typeof(IUserAccessor), typeof(UserAccessor));
        services.AddScoped(typeof(IGroupService), typeof(GroupService));
        services.AddScoped(typeof(IAuthService), typeof(AuthService));
        services.AddScoped(typeof(IBaseService), typeof(ServiceBase));
        services.AddScoped(typeof(IMailService), typeof(MailService));
        services.AddScoped(typeof(IFileManager), typeof(FileManager));

        //services.AddScoped<HistoryFileFilter>();
        //services.AddScoped<ActionFilter>();
    }
}
