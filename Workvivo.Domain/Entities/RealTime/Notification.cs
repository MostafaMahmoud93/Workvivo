using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.RealTime
{
    /// <summary>
    /// One thing that happened, worth telling people about.
    ///
    /// A single row fans out to many recipients through <see cref="NotificationUser"/>.
    /// An announcement to a hundred thousand staff is therefore one copy of the text and
    /// a hundred thousand narrow recipient rows, not a hundred thousand copies of the
    /// body.
    ///
    /// Copy is stored in both languages at write time rather than rendered per reader.
    /// The text quotes names and titles that can change afterwards, and a notification
    /// should say what was true when it was sent.
    /// </summary>
    public class Notification : FullBaseEntity<Guid>
    {
        public string Header_Ar { get; set; }
        public string Header_En { get; set; }
        public string Content_Ar { get; set; }
        public string Content_En { get; set; }
        public string? RedirectUrl { get; set; }
        public int Notification_Type { get; set; } = 1;
        public string Notification_Status { get; set; } = "OPEN";

        /// <summary>
        /// Who caused it, as an employee rather than a login.
        ///
        /// <see cref="FullBaseEntity{U}.Created_By"/> is the account that wrote the row,
        /// which for anything raised by a background job is nobody. The actor is what the
        /// client shows an avatar for, so it is held explicitly and is null for
        /// system-generated notifications.
        /// </summary>
        public Guid? Actor_Employee_Id { get; set; }

        /// <summary>What the notification points at. Drives the deep link and grouping.</summary>
        public NotificationEntityType Entity_Type { get; set; } = NotificationEntityType.None;

        public Guid? Entity_Id { get; set; }

        /// <summary>
        /// The login behind the notification, when there is one.
        ///
        /// Separate from <see cref="FullBaseEntity{U}.Created_By"/>, which the DbContext
        /// stamps for auditing and which cannot be null. This one can be, because a
        /// birthday reminder or a nightly digest is written by a background worker with
        /// nobody signed in - and satisfying a foreign key by inventing a system account
        /// puts a user in the audit trail who never did anything.
        /// </summary>
        public Guid? Creator_User_Id { get; set; }

        /// <summary>The person whose name and face the notification shows.</summary>
        public virtual Organization.Employee? Actor { get; set; }

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
