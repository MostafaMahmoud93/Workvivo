using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Surveys;

/// <summary>A choice offered by a single- or multiple-choice question.</summary>
public class SurveyQuestionOption : BaseCommonEntity<Guid>
{
    public Guid Question_Id { get; set; }
    public string Text_Ar { get; set; } = string.Empty;
    public string? Text_En { get; set; }
    public int Sort_Order { get; set; }

    public virtual SurveyQuestion? Question { get; set; }
    public virtual ICollection<SurveyAnswer> Answers { get; set; } = [];

    [NotMapped]
    public string? Text => LocalizedText.Pick(Text_Ar, Text_En);
}
