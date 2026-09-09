namespace Workvivo.Infrastructure.Configuration
{
    public class UserLoginLogConfig : IEntityTypeConfiguration<UserLoginLog>
    {
        public void Configure(EntityTypeBuilder<UserLoginLog> builder)
        {
            builder.HasOne(q => q.User).WithMany(x => x.UserLoginLogs).HasForeignKey(q => q.UserId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
