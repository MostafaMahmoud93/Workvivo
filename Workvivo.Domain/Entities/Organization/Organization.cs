using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;

namespace Workvivo.Domain.Entities.Organization;

/// <summary>
/// The company the platform runs for.
///
/// Modelled as a row rather than a setting because multi-entity groups are common in
/// this region - a holding company with several operating subsidiaries under one
/// intranet - and retrofitting a tenant later touches every table.
/// </summary>
public class Organization : FullBaseEntity<Guid>
{
    public string Name_Ar { get; set; } = string.Empty;
    public string? Name_En { get; set; }
    public string? Legal_Name { get; set; }
    public string? Description_Ar { get; set; }
    public string? Description_En { get; set; }
    public string? Website { get; set; }
    public Guid? Logo_File_Id { get; set; }
    public bool Is_Active { get; set; } = true;

    public virtual ICollection<Department> Departments { get; set; } = [];
    public virtual ICollection<Location> Locations { get; set; } = [];
    public virtual ICollection<JobTitle> JobTitles { get; set; } = [];

    [NotMapped]
    public string? Name => LocalizedText.Pick(Name_Ar, Name_En);

    [NotMapped]
    public string? Description => LocalizedText.Pick(Description_Ar, Description_En);
}
