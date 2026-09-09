using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>
/// A person's profile: who they are, where they sit, and what colleagues see.
///
/// Deliberately separate from <see cref="ApplicationUser"/>, one-to-one. The user row
/// is a credential - username, password hash, security stamp - while this is HR and
/// social data. Keeping them apart means an employee record can exist before the
/// account is provisioned (or after it is disabled), and an SSO migration that
/// replaces how people authenticate does not touch the org chart, the feed, or a
/// single post's authorship.
/// </summary>
public class Employee : FullBaseEntity<Guid>
{
    /// <summary>The login this profile belongs to. Unique - one profile per account.</summary>
    public Guid User_Id { get; set; }

    /// <summary>HR's identifier for the person. Unique, and what integrations key on.</summary>
    public string Employee_Number { get; set; } = string.Empty;

    public string First_Name { get; set; } = string.Empty;
    public string? Middle_Name { get; set; }
    public string Last_Name { get; set; } = string.Empty;

    /// <summary>
    /// What colleagues are shown. Held rather than derived because people go by names
    /// their HR record does not contain, and a computed "First Last" gets that wrong
    /// for a large share of any real workforce.
    /// </summary>
    public string Display_Name { get; set; } = string.Empty;

    public string? Full_Name_Ar { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Extension { get; set; }

    public Guid? Profile_Picture_File_Id { get; set; }
    public Guid? Cover_Picture_File_Id { get; set; }

    public Guid? Department_Id { get; set; }
    public Guid? Team_Id { get; set; }
    public Guid? Location_Id { get; set; }
    public Guid? Job_Title_Id { get; set; }

    public string? Biography_Ar { get; set; }
    public string? Biography_En { get; set; }

    public DateOnly? Joining_Date { get; set; }

    /// <summary>
    /// Date of birth, for the birthdays panel.
    ///
    /// Personal data: the feed shows the day and month, never the year, and never the
    /// value for someone who has opted out. That filtering is the query's job - the
    /// column simply holds what HR provided.
    /// </summary>
    public DateOnly? Birth_Date { get; set; }

    public bool Show_Birthday { get; set; } = true;

    /// <summary>UI language preference, "ar" or "en".</summary>
    public string Preferred_Language { get; set; } = "en";

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public bool Is_Active { get; set; } = true;

    /// <summary>Denormalised counters, so a profile card is one row rather than three counts.</summary>
    public int Followers_Count { get; set; }
    public int Following_Count { get; set; }
    public int Recognition_Points { get; set; }

    /// <summary>
    /// Concurrency token. HR edits and self-service profile edits genuinely collide,
    /// and the loser should be told rather than silently overwritten.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    public virtual ApplicationUser? User { get; set; }
    public virtual Department? Department { get; set; }
    public virtual Team? Team { get; set; }
    public virtual Location? Location { get; set; }
    public virtual JobTitle? JobTitle { get; set; }

    public virtual ICollection<EmployeeManager> Managers { get; set; } = [];
    public virtual ICollection<EmployeeManager> DirectReports { get; set; } = [];
    public virtual ICollection<EmployeeSkill> Skills { get; set; } = [];
    public virtual ICollection<EmployeeInterest> Interests { get; set; } = [];
    public virtual ICollection<EmployeeFollower> Followers { get; set; } = [];
    public virtual ICollection<EmployeeFollower> Following { get; set; } = [];

    /// <summary>
    /// Full name in the current language: the Arabic name when reading right-to-left,
    /// otherwise the display name.
    /// </summary>
    [NotMapped]
    public string? Name => LocalizedText.Pick(Full_Name_Ar, Display_Name);

    [NotMapped]
    public string? Biography => LocalizedText.Pick(Biography_Ar, Biography_En);
}
