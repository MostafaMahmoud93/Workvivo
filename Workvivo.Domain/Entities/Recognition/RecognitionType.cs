using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Recognition;

/// <summary>
/// A recognition category - Outstanding Performance, Teamwork, Innovation and so on.
///
/// A table rather than an enum because the categories, their badges and their point
/// values are exactly what an HR team wants to change without a deployment.
/// </summary>
public class RecognitionType : FullBaseEntity<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name_Ar { get; set; } = string.Empty;
    public string? Name_En { get; set; }
    public string? Description_Ar { get; set; }
    public string? Description_En { get; set; }

    public string? Badge_Icon { get; set; }
    public string? Badge_Color { get; set; }

    /// <summary>Points awarded unless the sender is allowed to override them.</summary>
    public int Default_Points { get; set; }

    public bool Is_Active { get; set; } = true;
    public int Sort_Order { get; set; }

    public virtual ICollection<Recognition> Recognitions { get; set; } = [];

    [NotMapped]
    public string? Name => LocalizedText.Pick(Name_Ar, Name_En);

    [NotMapped]
    public string? Description => LocalizedText.Pick(Description_Ar, Description_En);
}
