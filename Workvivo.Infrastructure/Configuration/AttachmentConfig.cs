namespace Workvivo.Infrastructure.Configuration
{
    public class AttachmentConfig : IEntityTypeConfiguration<GlobalAttachment>
    {
        public void Configure(EntityTypeBuilder<GlobalAttachment> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(q => q.User).WithMany(x => x.Attachments).HasForeignKey(q => q.CreatedBy).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
