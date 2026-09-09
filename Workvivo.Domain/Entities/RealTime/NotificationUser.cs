using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.RealTime
{
    public class NotificationUser : BaseCommonEntity<Guid>
    {
        public Guid Notification_Id { get; set; }
        public Guid Reciever_Id { get; set; }
        public bool IS_Seen { get; set; } = false;
        public virtual Notification Notification { get; set; }
        public virtual ApplicationUser Reciever { get; set; }
    }
}
