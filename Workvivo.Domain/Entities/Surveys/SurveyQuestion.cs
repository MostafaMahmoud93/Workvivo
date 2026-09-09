using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Surveys;

/// <summary>
/// One question. Its <see cref="SurveyQuestionType"/> decides which column on an
/// answer carries the value and whether options are expected.
/// </summary>
public class SurveyQuestion : FullBaseEntity<Guid>
{
    public Guid Survey_Id { get; set; }

    public SurveyQuestionType Question_Type { get; set; }
    public string Text_Ar { get; set; } = string.Empty;
    public string? Text_En { get; set; }
    public string? Help_Text_Ar { get; set; }
    public string? Help_Text_En { get; set; }

    public bool Is_Required { get; set; }
    public int Sort_Order { get; set; }

    /// <summary>Bounds for Rating and Scale. NPS is fixed at 0 to 10 and ignores them.</summary>
    public int? Min_Value { get; set; }
    public int? Max_Value { get; set; }
    public string? Min_Label_Ar { get; set; }
    public string? Min_Label_En { get; set; }
    public string? Max_Label_Ar { get; set; }
    public string? Max_Label_En { get; set; }

    public virtual Survey? Survey { get; set; }
    public virtual ICollection<SurveyQuestionOption> Options { get; set; } = [];
    public virtual ICollection<SurveyAnswer> Answers { get; set; } = [];

    [NotMapped]
    public string? Text => LocalizedText.Pick(Text_Ar, Text_En);

    /// <summary>Whether this question type expects option rows.</summary>
    public bool ExpectsOptions =>
        Question_Type is SurveyQuestionType.SingleChoice or SurveyQuestionType.MultipleChoice;
}
