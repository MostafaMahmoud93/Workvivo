using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Polls;

/// <summary>One answer choice.</summary>
public class PollOption : BaseCommonEntity<Guid>
{
    public Guid Poll_Id { get; set; }
    public string Text_Ar { get; set; } = string.Empty;
    public string? Text_En { get; set; }
    public int Sort_Order { get; set; }

    /// <summary>Denormalised tally, so rendering results is one row per option.</summary>
    public int Votes_Count { get; set; }

    public virtual Poll? Poll { get; set; }
    public virtual ICollection<PollVote> Votes { get; set; } = [];

    [NotMapped]
    public string? Text => LocalizedText.Pick(Text_Ar, Text_En);
}
