using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Common;

namespace Workvivo.Domain.Entities.Identity
{
    public class UserPermissions : BaseCommonEntity<Guid>
    {
        public Guid User_Id { get; set; }
        public Guid Link_Screen_Action_Id { get; set; }

        /// <summary>
        /// True grants this permission to the user directly; false denies it even when
        /// one of their roles grants it.
        ///
        /// Defaults to true so every row the template already wrote keeps its original
        /// meaning. Deny wins over any role grant - the safe direction, because the
        /// failure mode of the opposite rule is someone keeping access after it was
        /// explicitly taken away.
        /// </summary>
        public bool Is_Granted { get; set; } = true;
        public virtual LinkScreenAction Action { get; set; }
        public virtual ApplicationUser User { get; set; }
    }
}
