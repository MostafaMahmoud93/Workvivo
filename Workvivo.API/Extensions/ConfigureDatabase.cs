namespace Workvivo.API.Extensions;
public static class ConfigureDatabase
{
    public static void ConfigureDatabases(IServiceCollection services, IConfiguration Configuration)
    {
        services.AddDbContext<Workvivo_DbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("WorkvivoConnStr")));

        services.AddIdentity<ApplicationUser, UserGroup>().AddEntityFrameworkStores<Workvivo_DbContext>().AddDefaultTokenProviders();
    }
}
