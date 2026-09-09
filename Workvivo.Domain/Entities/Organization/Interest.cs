using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>
/// A non-work interest - football, photography, volunteering.
///
/// Kept apart from <see cref="Skill"/> because the two are used for opposite things:
/// skills answer "who can help with this", interests drive community suggestions and
/// the social side of the product. Merging them would make both searches worse.
/// </summary>
public class Interest : FullBaseEntity<Guid>
{
    public string Name_Ar { get; set; } = string.Empty;
    public string? Name_En { get; set; }
    public string? Icon { get; set; }
    public bool Is_Approved { get; set; }
    public int Usage_Count { get; set; }

    public virtual ICollection<EmployeeInterest> Employees { get; set; } = [];

    [NotMapped]
    public string? Name => LocalizedText.Pick(Name_Ar, Name_En);
}
