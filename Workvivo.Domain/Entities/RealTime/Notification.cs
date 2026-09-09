using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.RealTime
{
    public class Notification : FullBaseEntity<Guid>
    {
        public string Header_Ar { get; set; }
        public string Header_En { get; set; }
        public string Content_Ar { get; set; }
        public string Content_En { get; set; }
        public string? RedirectUrl { get; set; }
        public int Notification_Type { get; set; } = 1;
        public string Notification_Status { get; set; } = "OPEN";
        public virtual List<NotificationUser> NotificationUsers { get; set; } = new List<NotificationUser>();
        public virtual ApplicationUser CreatorUser { get; set; }

        [NotMapped]
        public string? SenderProfilePictureURL { get; set; }

        [NotMapped]
        public string? Header
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? Header_Ar : Header_En ?? Header_Ar;
            }
            set { }
        }
        [NotMapped]
        public string? Content
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft ? Content_Ar : Content_En ?? Content_Ar;
            }
            set { }
        }

    }
}
