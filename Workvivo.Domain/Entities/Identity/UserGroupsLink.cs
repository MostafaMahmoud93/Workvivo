namespace Workvivo.Domain.Entities.Identity;
public class UserGroupsLink : IdentityUserRole<Guid>
{
    public virtual ApplicationUser User { get; set; }
    public virtual UserGroup Role { get; set; }
}
