using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Polls;

/// <summary>
/// A quick single- or multiple-choice question, usually attached to a feed post.
///
/// Separate from <c>Survey</c> because the two are different products: a poll is one
/// question answered inline in the feed, a survey is an instrument with sections,
/// required questions and exportable results.
/// </summary>
public class Poll : AuditableEntity<Guid>
{
    /// <summary>The post carrying this poll. Null for a standalone poll.</summary>
    public Guid? Post_Id { get; set; }

    public string Question_Ar { get; set; } = string.Empty;
    public string? Question_En { get; set; }

    public bool Is_Multiple_Choice { get; set; }

    /// <summary>
    /// When set, votes are stored without the voter's id.
    ///
    /// It has to be fixed before the first vote: flipping it afterwards would either
    /// retroactively expose people who answered in confidence, or discard the
    /// attribution of those who did not.
    /// </summary>
    public bool Is_Anonymous { get; set; }

    /// <summary>Whether people can see results before they have voted.</summary>
    public bool Show_Results_Before_Voting { get; set; }

    public PollStatus Status { get; set; } = PollStatus.Draft;
    public DateTime? Start_Date { get; set; }
    public DateTime? Expiry_Date { get; set; }

    public int Total_Votes { get; set; }

    public virtual Post? Post { get; set; }
    public virtual ICollection<PollOption> Options { get; set; } = [];
    public virtual ICollection<PollVote> Votes { get; set; } = [];
    public virtual ICollection<PollAudience> Audiences { get; set; } = [];

    [NotMapped]
    public string? Question => LocalizedText.Pick(Question_Ar, Question_En);

    public bool IsOpenAt(DateTime utcNow) =>
        !Is_Deleted
        && Status == PollStatus.Open
        && (Start_Date is null || Start_Date <= utcNow)
        && (Expiry_Date is null || Expiry_Date > utcNow);
}
