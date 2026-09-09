using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>A skill claimed by an employee, with how many colleagues have endorsed it.</summary>
public class EmployeeSkill : BaseCommonEntity<Guid>
{
    public Guid Employee_Id { get; set; }
    public Guid Skill_Id { get; set; }
    public int Endorsement_Count { get; set; }
    public int Sort_Order { get; set; }
    public DateTime Added_At { get; set; }

    public virtual Employee? Employee { get; set; }
    public virtual Skill? Skill { get; set; }
}
