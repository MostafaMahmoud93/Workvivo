using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Surveys;

/// <summary>Targeting rule for a survey. See <see cref="AudienceEntity{TKey}"/>.</summary>
public class SurveyAudience : AudienceEntity<Guid>
{
    public SurveyAudience()
    {
    }

    public SurveyAudience(Guid surveyId, AudienceType audienceType, Guid? targetId)
        : base(audienceType, targetId)
    {
        Survey_Id = surveyId;
    }

    public Guid Survey_Id { get; set; }

    public virtual Survey? Survey { get; set; }
}
