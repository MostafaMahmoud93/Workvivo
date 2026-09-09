namespace Workvivo.Infrastructure.Configuration
{
    public class MainModuleConfig : IEntityTypeConfiguration<MainModule>
    {
        public void Configure(EntityTypeBuilder<MainModule> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasMany(p => p.Screens).WithOne(p => p.MainModule).HasForeignKey(p => p.Main_Module_Id);
        }
    }

    public class ScreenConfig : IEntityTypeConfiguration<Screen>
    {
        public void Configure(EntityTypeBuilder<Screen> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasMany(q => q.LinkScreenActions).WithOne(x => x.Screen).HasForeignKey(q => q.Screen_Id);
            builder.HasMany(q => q.SubScreens).WithOne(x => x.ParentScreen).HasForeignKey(q => q.Parent_Screen_Id);
        }
    }
    public class ScreenActionConfig : IEntityTypeConfiguration<ScreenAction>
    {
        public void Configure(EntityTypeBuilder<ScreenAction> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasMany(q => q.LinkScreenActions).WithOne(x => x.ScreenAction).HasForeignKey(q => q.Screen_Action_Id).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
