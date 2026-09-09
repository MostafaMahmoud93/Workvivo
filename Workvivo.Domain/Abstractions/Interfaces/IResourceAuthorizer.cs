namespace Workvivo.Domain.Abstractions.Interfaces;

/// <summary>
/// Decides whether a user may act on one specific record.
///
/// A permission check answers "may this person edit posts". It cannot answer "may this
/// person edit <em>this</em> post", and that gap is where insecure direct object
/// reference lives: an author with Post.Edit passing somebody else's id and having it
/// accepted because the permission check passed.
///
/// Implementations live with their resource, since the rule is domain-specific - an
/// author may edit their own post, a moderator may edit any post in a community they
/// moderate.
/// </summary>
public interface IResourceAuthorizer<in TResource>
{
    Task<bool> CanAsync(Guid userId, TResource resource, string operation, CancellationToken cancellationToken = default);
}

/// <summary>Operations a resource authorizer is asked about.</summary>
public static class ResourceOperations
{
    public const string Read = "read";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string Moderate = "moderate";
}
