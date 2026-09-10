using Workvivo.Infrastructure.Configuration.Conventions;

namespace Workvivo.Infrastructure.Configuration
{
    public class NotificationConfig : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(x => x.Id);

            // Optional, and keyed on its own column rather than on Created_By.
            //
            // Created_By is the audit stamp: non-nullable, written by the DbContext for
            // every entity. Hanging a foreign key off it meant a notification could only
            // exist if a signed-in user created it, which a scheduled announcement or a
            // nightly digest cannot satisfy. The navigation on both sides is unchanged.
            builder.HasOne(q => q.CreatorUser)
                .WithMany(x => x.Notifications)
                .HasForeignKey(q => q.Creator_User_Id)
                .HasPrincipalKey(a => a.Id)
                .OnDelete(DeleteBehavior.Restrict);

            // NoAction, not Restrict or Cascade. An employee record being removed must
            // not delete the history of what they did, and must not block the removal
            // either - the notification simply loses its face.
            builder.HasOne(x => x.Actor)
                .WithMany()
                .HasForeignKey(x => x.Actor_Employee_Id)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(x => x.Notification_Status).HasMaxLength(ColumnLengths.Code);
            builder.Property(x => x.Header_Ar).HasMaxLength(ColumnLengths.Description);
            builder.Property(x => x.Header_En).HasMaxLength(ColumnLengths.Description);
            builder.Property(x => x.RedirectUrl).HasMaxLength(ColumnLengths.Url);

            // "Everything about this post", for the reconciliation and clean-up jobs and
            // for collapsing duplicates later.
            builder.HasIndex(x => new { x.Entity_Type, x.Entity_Id })
                .HasDatabaseName("IX_Notifications_Entity");
        }
    }
    public class NotificationUsersConfig : IEntityTypeConfiguration<NotificationUser>
    {
        public void Configure(EntityTypeBuilder<NotificationUser> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.Notification).WithMany(a => a.NotificationUsers).HasForeignKey(x => x.Notification_Id);
            builder.HasOne(x => x.Reciever).WithMany(a => a.NotificationUsers).HasForeignKey(x => x.Reciever_Id);

            // The one query that matters: "my notifications, newest first". Recipient
            // first because it is the equality predicate, date descending because that is
            // the sort - in that order the whole page is one seek plus a range scan, and
            // the unread count is answered from the same index.
            builder.HasIndex(x => new { x.Reciever_Id, x.Create_Date })
                .HasDatabaseName("IX_NotificationUsers_Recipient")
                .IsDescending(false, true);

            // Unread is a small slice of a large table, so it gets a filtered index of
            // its own rather than scanning the recipient's whole history to count zeroes.
            builder.HasIndex(x => x.Reciever_Id)
                .HasDatabaseName("IX_NotificationUsers_Unread")
                .HasFilter("[IS_Seen] = 0 AND [Is_Deleted] = 0");

            // A notification is delivered to a person once. Without this, a retried
            // fan-out job silently doubles everyone's bell.
            builder.HasIndex(x => new { x.Notification_Id, x.Reciever_Id })
                .IsUnique()
                .HasDatabaseName("UX_NotificationUsers_Delivery");
        }
    }
}
