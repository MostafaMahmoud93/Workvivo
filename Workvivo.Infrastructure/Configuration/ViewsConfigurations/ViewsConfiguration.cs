namespace Workvivo.Infrastructure.Configuration.ViewsConfigurations;

public class ViewsConfiguration
{
    public static void Configuration(ModelBuilder builder)
    {
        // ToView, not just HasNoKey.
        //
        // Without it EF treats VW_UserActions as an ordinary entity and the migration
        // creates a TABLE of that name - which is exactly what happened: the table was
        // created empty, nothing ever wrote to it, and every permission check therefore
        // resolved to "no permissions" while looking perfectly healthy.
        //
        // The view itself is created by SQL in the migration, since EF has no model for
        // view bodies.
        builder.Entity<VW_UserActions>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("VW_UserActions");
        });
    }
}
