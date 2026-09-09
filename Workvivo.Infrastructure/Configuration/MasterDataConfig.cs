namespace Workvivo.Infrastructure.Configuration
{
    public class MasterDataConfig : IEntityTypeConfiguration<MasterData>
    {
        public void Configure(EntityTypeBuilder<MasterData> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.HasMany(q => q.MasterDataChilds).WithOne(x => x.MasterDataParent).HasForeignKey(q => q.Master_Data_Parent_Id);
            builder.HasMany(q => q.ApplicationUserUserType).WithOne(x => x.MastarDataUserType).HasForeignKey(q => q.User_Type).HasPrincipalKey(a => a.Master_Data_Code);
            builder.HasMany(q => q.UserGroups).WithOne(x => x.UserType).HasForeignKey(q => q.User_Type).HasPrincipalKey(a => a.Master_Data_Code).OnDelete(DeleteBehavior.Restrict);
        }
    }

}
