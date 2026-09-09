using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>A team inside a department.</summary>
public class Team : FullBaseEntity<Guid>
{
    public Guid Department_Id { get; set; }

    public string Name_Ar { get; set; } = string.Empty;
    public string? Name_En { get; set; }
    public string? Description_Ar { get; set; }
    public string? Description_En { get; set; }
    public Guid? Lead_Employee_Id { get; set; }
    public bool Is_Active { get; set; } = true;

    public virtual Department? Department { get; set; }
    public virtual Employee? Lead { get; set; }
    public virtual ICollection<Employee> Employees { get; set; } = [];

    [NotMapped]
    public string? Name => LocalizedText.Pick(Name_Ar, Name_En);
}
