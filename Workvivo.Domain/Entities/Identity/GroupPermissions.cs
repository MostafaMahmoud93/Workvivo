using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Common;

namespace Workvivo.Domain.Entities.Identity
{
    public class GroupPermissions : BaseCommonEntity<Guid>
    {
        public Guid Link_Screen_Action_Id { get; set; }
        public Guid Group_Id { get; set; }
        public virtual LinkScreenAction Action { get; set; }
        public virtual UserGroup Role { get; set; }
    }
}
