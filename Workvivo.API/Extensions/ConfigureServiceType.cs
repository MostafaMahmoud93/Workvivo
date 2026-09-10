using Workvivo.Application.Features.Auth.Services;
using Workvivo.Infrastructure.Identity;
using Workvivo.Application.Features.Communities.Common;
using Workvivo.Application.Features.Posts.Common;
using Workvivo.Infrastructure.Feed;
using Workvivo.Infrastructure.Seeding;

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

        // Authentication and authorisation (phase 4).
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuthSessionFactory, AuthSessionFactory>();
        services.AddScoped<DevelopmentDataSeeder>();

        // Feed (phase 6).
        services.AddScoped<IAudienceResolver, AudienceResolver>();
        services.AddScoped<PostAuthorization>();
        services.AddScoped<CommunityAuthorization>();

        // A policy provider rather than a policy per permission: permissions are rows
        // in a table, so the set is not known at start-up.
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        //services.AddScoped<HistoryFileFilter>();
        //services.AddScoped<ActionFilter>();
    }
}
