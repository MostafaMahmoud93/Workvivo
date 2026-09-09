using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Surveys;

/// <summary>One person's submission.</summary>
public class SurveyResponse : BaseCommonEntity<Guid>
{
    public Guid Survey_Id { get; set; }

    /// <summary>Null on an anonymous survey.</summary>
    public Guid? Employee_Id { get; set; }

    /// <summary>
    /// Keyed hash of the respondent, on anonymous surveys only - the same trick as a
    /// poll's voter hash, and for the same reason: it enforces one response per person
    /// without recording who they were.
    /// </summary>
    public string? Respondent_Hash { get; set; }

    public DateTime Started_At { get; set; }
    public DateTime? Submitted_At { get; set; }

    /// <summary>False while a partial save is still in progress.</summary>
    public bool Is_Complete { get; set; }

    /// <summary>
    /// Department and location captured at submission time.
    ///
    /// Copied rather than joined through the employee, because analytics has to keep
    /// meaning after a reorganisation: results for "Finance in March" must not shift
    /// when someone transfers out in April - and on an anonymous survey there is no
    /// employee row to join to at all.
    /// </summary>
    public Guid? Department_Id_At_Submission { get; set; }
    public Guid? Location_Id_At_Submission { get; set; }

    public virtual Survey? Survey { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual ICollection<SurveyAnswer> Answers { get; set; } = [];
}
