namespace Workvivo.Infrastructure.Configuration
{
    public class EmailSMSTemplateConfig : IEntityTypeConfiguration<EmailSMSTemplate>
    {
        public void Configure(EntityTypeBuilder<EmailSMSTemplate> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(q => q.Screen).WithMany(x => x.EmailSMSTemplates).HasForeignKey(q => q.Screen_Id);
        }
    }
    public class EmailSMSHistoryConfig : IEntityTypeConfiguration<EmailSMSHistory>
    {
        public void Configure(EntityTypeBuilder<EmailSMSHistory> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(q => q.EmailSMSTemplate).WithMany(x => x.EmailSMSHistories).HasForeignKey(q => q.Template_Id);
        }
    }
}
