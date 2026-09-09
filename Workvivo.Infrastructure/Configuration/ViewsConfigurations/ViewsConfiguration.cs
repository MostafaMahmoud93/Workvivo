namespace Workvivo.Infrastructure.Configuration.ViewsConfigurations;
public class ViewsConfiguration
{
    public static void Configuration(ModelBuilder builder)
    {
        builder.Entity<VW_UserActions>(p => p.HasNoKey());
    }
}