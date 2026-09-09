using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Common;

namespace Workvivo.Domain.Entities.Identity
{
    public class UserPermissions : BaseCommonEntity<Guid>
    {
        public Guid User_Id { get; set; }
        public Guid Link_Screen_Action_Id { get; set; }
        public virtual LinkScreenAction Action { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}
