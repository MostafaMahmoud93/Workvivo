namespace Workvivo.Infrastructure.Configuration
{
    public class NotificationConfig : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(q => q.CreatorUser).WithMany(x => x.Notifications).HasForeignKey(q => q.Created_By).HasPrincipalKey(a => a.Id).OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class NotificationUsersConfig : IEntityTypeConfiguration<NotificationUser>
    {
        public void Configure(EntityTypeBuilder<NotificationUser> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.Notification).WithMany(a => a.NotificationUsers).HasForeignKey(x => x.Notification_Id);
            builder.HasOne(x => x.Reciever).WithMany(a => a.NotificationUsers).HasForeignKey(x => x.Reciever_Id);
        }
    }
}
