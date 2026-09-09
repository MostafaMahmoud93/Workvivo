namespace Workvivo.API.Extensions;
public static class ConfigureRepositoriesType
{
    public static void AddRepositoriesLayer(IServiceCollection services)
    {
        services.AddScoped(typeof(ICustomBaseRepository<>), typeof(CustomBaseRepository<>));
        services.AddScoped(typeof(IBaseRepository<,>), typeof(BaseRepository<,>));
        services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
    }
}
