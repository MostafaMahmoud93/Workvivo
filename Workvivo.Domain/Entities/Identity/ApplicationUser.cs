using Workvivo.Domain.Entities.Common;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Entities.RealTime;

namespace Workvivo.Domain.Entities.Identity;
public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        UserRoles = new HashSet<UserGroupsLink>();
        UserActions = new HashSet<UserPermissions>();
        NotificationUsers = new HashSet<NotificationUser>();
        Notifications = new HashSet<Notification>();
    }
    public bool Is_Admin { get; set; }
    public bool Is_Active { get; set; }
    public bool Is_Deleted { get; set; }
    public Guid? Created_By { get; set; }
    public DateTime Create_Date { get; set; }
    public Guid? Last_Modified_By { get; set; }
    public DateTime? Last_Modify_Date { get; set; }
    public string? User_Type { get; set; }
    public string? Profile_PictureURL { get; set; }
    public string? Signee_PictureURL { get; set; }
    public string Full_Name_Ar { get; set; }
    public string? Full_Name_En { get; set; }
    public string? MobileNo { get; set; }
    public string? Office_TelNo { get; set; }
    public string? JobTitle { get; set; }
    public int? DepartmentNo { get; set; }
    public int? ManagNo { get; set; }
    public bool Is_Manager { get; set; }
    public virtual MasterData MastarDataUserType { get; set; }
    public virtual ICollection<GlobalAttachment>? Attachments { get; set; }
    public virtual ICollection<UsersShortCuts>? UsersShortCuts { get; set; }
    public virtual ICollection<Notification> Notifications { get; set; }
    public virtual ICollection<UserPermissions> UserActions { get; set; }
    public virtual ICollection<NotificationUser> NotificationUsers { get; set; }
    public virtual ICollection<UserGroupsLink> UserRoles { get; set; }
    public virtual ICollection<AccessLog> AccessLogs { get; set; }
    public virtual ICollection<UserLoginLog> UserLoginLogs { get; set; }

    /// <summary>
    /// The HR and social profile for this login, one-to-one.
    ///
    /// Nullable because the two genuinely come apart: a service account has no
    /// employee record, and an employee record can be created by an HR import before
    /// anyone provisions the account.
    /// </summary>
    public virtual Employee? Employee { get; set; }

    /// <summary>Refresh tokens issued to this user, including revoked ones.</summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    [NotMapped]
    public string? Name
    {
        get
        {
            return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? Full_Name_Ar : Full_Name_En ?? Full_Name_Ar;
        }
        set { }
    }
}
