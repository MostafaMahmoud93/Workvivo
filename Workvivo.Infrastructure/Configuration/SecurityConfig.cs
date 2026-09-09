namespace Workvivo.Infrastructure.Configuration;
public class UserActionConfig : IEntityTypeConfiguration<UserPermissions>
{
    public void Configure(EntityTypeBuilder<UserPermissions> builder)
    {
        builder.HasOne(q => q.Action).WithMany(x => x.UserActions).HasForeignKey(q => q.Link_Screen_Action_Id);
        builder.HasOne(q => q.User).WithMany(x => x.UserActions).HasForeignKey(q => q.User_Id);
    }
}

public class RoleActionConfig : IEntityTypeConfiguration<GroupPermissions>
{
    public void Configure(EntityTypeBuilder<GroupPermissions> builder)
    {
        builder.HasOne(q => q.Action).WithMany(x => x.GroupActions).HasForeignKey(q => q.Link_Screen_Action_Id);
        builder.HasOne(q => q.Role).WithMany(x => x.RoleActions).HasForeignKey(q => q.Group_Id);
    }
}
