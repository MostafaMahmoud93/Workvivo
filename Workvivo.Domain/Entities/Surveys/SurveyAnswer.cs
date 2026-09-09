using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Surveys;

/// <summary>
/// One answer within a response. Multiple-choice questions produce one row per
/// selected option.
/// </summary>
public class SurveyAnswer : BaseCommonEntity<Guid>
{
    public Guid Response_Id { get; set; }
    public Guid Question_Id { get; set; }

    /// <summary>Set for choice questions.</summary>
    public Guid? Option_Id { get; set; }

    /// <summary>Set for free-text questions. Sanitised on write.</summary>
    public string? Text_Value { get; set; }

    /// <summary>Set for Rating, Scale, NPS, and for Yes/No as 1 and 0.</summary>
    public int? Numeric_Value { get; set; }

    public virtual SurveyResponse? Response { get; set; }
    public virtual SurveyQuestion? Question { get; set; }
    public virtual SurveyQuestionOption? Option { get; set; }
}
