using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Documents;

/// <summary>
/// A folder in the document centre - HR Policies, IT Policies, Forms, Guidelines.
/// Self-nesting.
/// </summary>
public class DocumentCategory : FullBaseEntity<Guid>
{
    public Guid? Parent_Category_Id { get; set; }

    public string Name_Ar { get; set; } = string.Empty;
    public string? Name_En { get; set; }
    public string? Description_Ar { get; set; }
    public string? Description_En { get; set; }

    public string? Icon { get; set; }
    public int Sort_Order { get; set; }
    public bool Is_Active { get; set; } = true;

    public virtual DocumentCategory? ParentCategory { get; set; }
    public virtual ICollection<DocumentCategory> ChildCategories { get; set; } = [];
    public virtual ICollection<Document> Documents { get; set; } = [];

    [NotMapped]
    public string? Name => LocalizedText.Pick(Name_Ar, Name_En);

    [NotMapped]
    public string? Description => LocalizedText.Pick(Description_Ar, Description_En);
}
