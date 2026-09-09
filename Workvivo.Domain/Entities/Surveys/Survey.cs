using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Surveys;

/// <summary>An engagement or feedback survey.</summary>
public class Survey : AuditableEntity<Guid>
{
    public string Title_Ar { get; set; } = string.Empty;
    public string? Title_En { get; set; }
    public string? Description_Ar { get; set; }
    public string? Description_En { get; set; }

    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;

    /// <summary>
    /// Fixed once responses exist, for the same reason as on a poll: changing it
    /// afterwards either exposes people who answered in confidence or throws away
    /// attribution that was given knowingly.
    /// </summary>
    public bool Is_Anonymous { get; set; }

    public bool Allow_Multiple_Responses { get; set; }

    /// <summary>Whether a respondent can save and come back to finish later.</summary>
    public bool Allow_Partial_Save { get; set; } = true;

    public DateTime? Start_Date { get; set; }
    public DateTime? End_Date { get; set; }

    public int Response_Count { get; set; }

    /// <summary>
    /// Size of the targeted audience when the survey opened, so the participation rate
    /// is measured against the population that was actually invited rather than
    /// against today's headcount.
    /// </summary>
    public int Invited_Count { get; set; }

    public virtual ICollection<SurveyQuestion> Questions { get; set; } = [];
    public virtual ICollection<SurveyResponse> Responses { get; set; } = [];
    public virtual ICollection<SurveyAudience> Audiences { get; set; } = [];

    [NotMapped]
    public string? Title => LocalizedText.Pick(Title_Ar, Title_En);

    [NotMapped]
    public string? Description => LocalizedText.Pick(Description_Ar, Description_En);

    public bool IsOpenAt(DateTime utcNow) =>
        !Is_Deleted
        && Status == SurveyStatus.Published
        && (Start_Date is null || Start_Date <= utcNow)
        && (End_Date is null || End_Date > utcNow);
}
