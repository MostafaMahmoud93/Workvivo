using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>
/// A department, which may sit under another department.
/// </summary>
public class Department : FullBaseEntity<Guid>
{
    public Guid Organization_Id { get; set; }
    public Guid? Parent_Department_Id { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name_Ar { get; set; } = string.Empty;
    public string? Name_En { get; set; }
    public string? Description_Ar { get; set; }
    public string? Description_En { get; set; }

    /// <summary>
    /// The department head. Nullable because a department can legitimately be between
    /// managers, and blocking the org chart on that would be worse than allowing it.
    /// </summary>
    public Guid? Manager_Employee_Id { get; set; }

    /// <summary>
    /// Materialised path of ancestor ids, for example <c>/root/div/dept/</c>.
    ///
    /// Denormalised on purpose. "Everything under this division" is asked constantly -
    /// by audience targeting, the org chart, and every departmental analytics filter -
    /// and a recursive CTE per request does not hold up at 100,000 employees. A LIKE
    /// on an indexed prefix does. Maintained whenever the parent changes.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Depth in the hierarchy; 0 for a top-level department.</summary>
    public int Level { get; set; }

    public int Sort_Order { get; set; }
    public bool Is_Active { get; set; } = true;

    public virtual Organization? Organization { get; set; }
    public virtual Department? ParentDepartment { get; set; }
    public virtual Employee? Manager { get; set; }
    public virtual ICollection<Department> ChildDepartments { get; set; } = [];
    public virtual ICollection<Team> Teams { get; set; } = [];
    public virtual ICollection<Employee> Employees { get; set; } = [];

    [NotMapped]
    public string? Name => LocalizedText.Pick(Name_Ar, Name_En);

    [NotMapped]
    public string? Description => LocalizedText.Pick(Description_Ar, Description_En);
}
