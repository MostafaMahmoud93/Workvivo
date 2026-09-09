using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>
/// A skill from the shared catalogue.
///
/// Catalogued rather than free text on the employee so that "find me someone who
/// knows Kubernetes" is an indexed join, not a LIKE over every profile, and so
/// "K8s", "kubernetes" and "Kubernetes " are one thing instead of three.
/// </summary>
public class Skill : FullBaseEntity<Guid>
{
    public string Name_Ar { get; set; } = string.Empty;
    public string? Name_En { get; set; }
    public string? Category { get; set; }

    /// <summary>
    /// False for a skill a member of staff typed in that an administrator has not yet
    /// accepted into the catalogue. Lets people self-describe without letting the
    /// catalogue fill up with near-duplicates unattended.
    /// </summary>
    public bool Is_Approved { get; set; }

    public int Usage_Count { get; set; }

    public virtual ICollection<EmployeeSkill> Employees { get; set; } = [];

    [NotMapped]
    public string? Name => LocalizedText.Pick(Name_Ar, Name_En);
}
