namespace Workvivo.Infrastructure.Configuration
{
    public class LookupConfig : IEntityTypeConfiguration<Lookup>
    {
        public void Configure(EntityTypeBuilder<Lookup> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(q => q.LookupCategory).WithMany(x => x.Lookups).HasPrincipalKey(q => q.Code).HasForeignKey(a => a.Category_Code);
        }
    }
    public class LookupCategoryConfig : IEntityTypeConfiguration<LookupCategory>
    {
        public void Configure(EntityTypeBuilder<LookupCategory> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }
}
