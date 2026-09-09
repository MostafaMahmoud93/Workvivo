namespace Workvivo.Infrastructure.Configuration
{
    public class AccessLogConfig : IEntityTypeConfiguration<AccessLog>
    {
        public void Configure(EntityTypeBuilder<AccessLog> builder)
        {
            builder.HasOne(q => q.MainModule).WithMany(x => x.AccessLogs).HasForeignKey(q => q.Main_Module_Id);
            builder.HasOne(q => q.ApplicationUser).WithMany(x => x.AccessLogs).HasForeignKey(q => q.User_Id);
            builder.HasOne(q => q.LinkScreenAction).WithMany(x => x.AccessLogs).HasForeignKey(q => q.Action_Code).HasPrincipalKey(q => q.Action_Code);
        }
    }
}
