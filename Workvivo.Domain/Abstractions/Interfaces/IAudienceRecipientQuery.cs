namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// The reverse of <see cref="IAudienceResolver"/>: given something that carries
/// audience rules, who does it actually reach?
///
/// Needed because the feed answers "what should this person see", and an announcement
/// asks the opposite question - "who must be told about this". The two are different
/// queries against the same rules, and conflating them is how a product ends up
/// notifying the wrong set of people from the set it displays to.
///
/// Paged, and paged by key rather than by offset, because the honest answer for a
/// company-wide announcement is every employee. A method that returned a list would
/// work in development and allocate a hundred thousand ids in production.
/// </summary>
public interface IAudienceRecipientQuery
{
    /// <summary>
    /// One batch of employee ids the post reaches, ordered by id.
    /// </summary>
    /// <param name="afterEmployeeId">
    /// Exclusive lower bound - the last id of the previous batch, or null to start.
    /// </param>
    Task<IReadOnlyList<Guid>> GetPostRecipientsAsync(
        Guid postId,
        Guid? afterEmployeeId,
        int batchSize,
        CancellationToken cancellationToken = default);
}
