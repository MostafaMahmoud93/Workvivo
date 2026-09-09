using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Common
{
    public class LinkScreenAction : BaseCommonEntity<Guid>
    {
        public LinkScreenAction()
        {
            GroupActions = new HashSet<GroupPermissions>();
            UserActions = new HashSet<UserPermissions>();
        }

        public Guid Screen_Id { get; set; }
        public Guid Screen_Action_Id { get; set; }
        public string Base_Route { get; set; }
        public string Action_Code { get; set; }

        /// <summary>
        /// Named permission this row grants, for example <c>Post.Create</c>.
        ///
        /// Added so the platform's permission checks and the template's screen/action
        /// engine share one catalogue instead of running two in parallel. The same row
        /// keeps driving the navigation menu through its Screen, while
        /// [HasPermission("Post.Create")] resolves against this column.
        ///
        /// Nullable: the rows the template already seeds are screen actions with no
        /// named equivalent, and backfilling them with invented names would be worse
        /// than leaving them null.
        /// </summary>
        public string? Permission_Key { get; set; }

        /// <summary>Grouping for the permission-management screen, for example <c>Feed</c>.</summary>
        public string? Module { get; set; }

        public string? Description_Ar { get; set; }
        public string? Description_En { get; set; }
        public virtual Screen Screen { get; set; }
        public virtual ScreenAction ScreenAction { get; set; }
        public virtual ICollection<GroupPermissions> GroupActions { get; set; }
        public virtual ICollection<UserPermissions> UserActions { get; set; }
        public virtual ICollection<AccessLog> AccessLogs { get; set; }
    }
}
