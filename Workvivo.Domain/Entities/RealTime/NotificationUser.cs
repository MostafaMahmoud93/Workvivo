using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.RealTime
{
    /// <summary>
    /// One person's copy of a notification: whether they have seen it, and when it
    /// reached them.
    /// </summary>
    public class NotificationUser : BaseCommonEntity<Guid>
    {
        public Guid Notification_Id { get; set; }
        public Guid Reciever_Id { get; set; }
        public bool IS_Seen { get; set; } = false;

        /// <summary>
        /// When this copy was created.
        ///
        /// Duplicated from the parent notification on purpose. The list every employee
        /// opens is "my notifications, newest first, unread first", and ordering by a
        /// column on the other side of a join cannot be served by one index - at which
        /// point the most-hit query in the product sorts a temporary table.
        /// </summary>
        public DateTime Create_Date { get; set; }

        /// <summary>When the recipient marked it read. Null while unread.</summary>
        public DateTime? Seen_Date { get; set; }

        public virtual Notification Notification { get; set; }
        public virtual ApplicationUser Reciever { get; set; }
    }
}
