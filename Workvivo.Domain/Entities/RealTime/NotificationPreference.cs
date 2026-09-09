using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.RealTime;

/// <summary>
/// One employee's delivery choices for one notification type.
///
/// A row per employee per type, rather than a blob of flags, so an administrator can
/// query "who still gets survey email" and a new notification type can be added
/// without rewriting everyone's settings.
///
/// Absence means the defaults apply. Rows are written only when somebody changes
/// something, which keeps the table proportional to the people who care rather than
/// to headcount times types.
/// </summary>
public class NotificationPreference : FullBaseEntity<Guid>
{
    public Guid Employee_Id { get; set; }

    public NotificationType Notification_Type { get; set; }

    public bool In_App_Enabled { get; set; } = true;
    public bool Email_Enabled { get; set; } = true;

    /// <summary>Reserved for mobile push; no sender is wired up yet.</summary>
    public bool Push_Enabled { get; set; }

    /// <summary>How often email for this type is batched.</summary>
    public NotificationDigestFrequency Email_Frequency { get; set; } = NotificationDigestFrequency.Immediate;

    public virtual Employee? Employee { get; set; }

    /// <summary>Whether this preference permits delivery on the given channel.</summary>
    public bool Allows(NotificationChannel channel) => channel switch
    {
        NotificationChannel.InApp => In_App_Enabled,
        NotificationChannel.Email => Email_Enabled && Email_Frequency != NotificationDigestFrequency.Never,
        NotificationChannel.Push => Push_Enabled,
        _ => false,
    };
}
