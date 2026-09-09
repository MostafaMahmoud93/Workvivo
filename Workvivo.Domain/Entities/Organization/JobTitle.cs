using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>A job title, shared across employees so it can be targeted and reported on.</summary>
public class JobTitle : FullBaseEntity<Guid>
{
    public Guid Organization_Id { get; set; }

    public string Name_Ar { get; set; } = string.Empty;
    public string? Name_En { get; set; }

    /// <summary>Optional grade or band, for reporting. Free text - grading schemes vary.</summary>
    public string? Grade { get; set; }

    public bool Is_Active { get; set; } = true;

    public virtual Organization? Organization { get; set; }
    public virtual ICollection<Employee> Employees { get; set; } = [];

    [NotMapped]
    public string? Name => LocalizedText.Pick(Name_Ar, Name_En);
}
