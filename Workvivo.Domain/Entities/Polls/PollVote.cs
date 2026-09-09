using Workvivo.Domain.Abstractions.Classes;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Communities;
using Workvivo.Domain.Entities.Documents;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;

namespace Workvivo.Domain.Entities.Polls;

/// <summary>
/// One vote for one option. A multiple-choice poll produces several rows per voter.
/// </summary>
public class PollVote : BaseCommonEntity<Guid>
{
    public Guid Poll_Id { get; set; }
    public Guid Option_Id { get; set; }

    /// <summary>Null on an anonymous poll. Anonymity has to mean the identity is absent.</summary>
    public Guid? Employee_Id { get; set; }

    /// <summary>
    /// Keyed hash of the voter's id, written only for anonymous polls.
    ///
    /// The problem it solves: an anonymous poll still has to stop one person voting a
    /// hundred times, which needs a per-voter unique value - but storing the id defeats
    /// the anonymity. A hash under a per-poll key gives uniqueness within this poll and
    /// nothing else: it cannot be matched against another poll's hashes, and cannot be
    /// reversed by trying every employee id without the key.
    /// </summary>
    public string? Voter_Hash { get; set; }

    public DateTime Voted_At { get; set; }

    public virtual Poll? Poll { get; set; }
    public virtual PollOption? Option { get; set; }
    public virtual Employee? Employee { get; set; }
}
