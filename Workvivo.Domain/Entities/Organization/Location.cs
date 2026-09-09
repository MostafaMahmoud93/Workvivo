using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>An office or site.</summary>
public class Location : FullBaseEntity<Guid>
{
    public Guid Organization_Id { get; set; }

    public string Name_Ar { get; set; } = string.Empty;
    public string? Name_En { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address_Ar { get; set; }
    public string? Address_En { get; set; }

    /// <summary>
    /// IANA time zone id, for example <c>Asia/Dubai</c>.
    ///
    /// Held per location because event times and reminders have to make sense to the
    /// person reading them: an all-hands at 14:00 Dubai is 11:00 for the London office,
    /// and storing only UTC loses the organiser's intent when a DST boundary moves.
    /// </summary>
    public string? TimeZone_Id { get; set; }

    public bool Is_Active { get; set; } = true;

    public virtual Organization? Organization { get; set; }
    public virtual ICollection<Employee> Employees { get; set; } = [];

    [NotMapped]
    public string? Name => LocalizedText.Pick(Name_Ar, Name_En);

    [NotMapped]
    public string? Address => LocalizedText.Pick(Address_Ar, Address_En);
}
