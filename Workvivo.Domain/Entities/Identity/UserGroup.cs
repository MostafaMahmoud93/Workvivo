using Workvivo.Domain.Entities.Common;

namespace Workvivo.Domain.Entities.Identity;
public class UserGroup : IdentityRole<Guid>
{
    public string Name_Ar { get; set; }
    public string Name_En { get; set; }
    public string User_Type { get; set; }
    public bool Is_Active { get; set; }
    public Guid Created_By { get; set; }
    public DateTime Create_Date { get; set; }
    public Guid? Last_Modify_By { get; set; }
    public DateTime? Last_Modify_Date { get; set; }
    public bool Is_Deleted { get; set; }
    public virtual MasterData UserType { get; set; }
    public virtual ICollection<UserGroupsLink> UserRoles { get; set; }
    public virtual ICollection<GroupPermissions> RoleActions { get; set; }
    [NotMapped]
    public string? GroupName
    {
        get
        {
            return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? Name_Ar : Name_En ?? Name_Ar;
        }
        set { }
    }
}
