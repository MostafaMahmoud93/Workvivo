using Microsoft.EntityFrameworkCore;
using Workvivo.Application.Common.Security;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Feed;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;
using Workvivo.Infrastructure.Seeding;

namespace Workvivo.Application.Features.Posts.Common;

/// <summary>
/// Answers "may this person act on <em>this</em> post".
///
/// A permission check answers "may they edit posts" and stops there. That gap is where
/// insecure direct object reference lives: an author holding Post.Edit passing somebody
/// else's post id and having it accepted, because the permission check passed and
/// nothing looked at the row.
///
/// Shared by every command that takes a post id, so the rule is stated once rather than
/// re-derived - slightly differently - in each handler.
/// </summary>
public sealed class PostAuthorization
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUser _currentUser;
    private readonly CurrentEmployee _currentEmployee;

    public PostAuthorization(
        IUnitOfWork unitOfWork,
        IPermissionService permissions,
        ICurrentUser currentUser,
        CurrentEmployee currentEmployee)
    {
        _unitOfWork = unitOfWork;
        _permissions = permissions;
        _currentUser = currentUser;
        _currentEmployee = currentEmployee;
    }

    /// <summary>
    /// The signed-in user's employee id, or a failure if they have no profile.
    ///
    /// Delegates to the shared resolver. The lookup started here and every later
    /// feature area needed it, so it moved rather than being copied - and the shared
    /// one caches per request, which this did not.
    /// </summary>
    public Task<Guid> RequireEmployeeIdAsync(CancellationToken cancellationToken) =>
        _currentEmployee.RequireIdAsync(cancellationToken);

    /// <summary>
    /// Loads a post the caller is allowed to change, or fails.
    ///
    /// Not found and not permitted both surface as <see cref="NotFoundException"/>.
    /// Distinguishing them would confirm that a given post id exists to somebody who
    /// cannot see it, which is how an id space gets enumerated.
    /// </summary>
    public async Task<Post> RequireEditableAsync(Guid postId, CancellationToken cancellationToken)
    {
        var employeeId = await RequireEmployeeIdAsync(cancellationToken);

        var post = await _unitOfWork.Repository<Post, Guid>().FindByIDAsync(postId)
            ?? throw new NotFoundException(nameof(Post), postId);

        if (post.Author_Employee_Id == employeeId)
        {
            return post;
        }

        var canModerate = await _permissions.HasPermissionAsync(
            _currentUser.UserId!.Value, PermissionKeys.PostModerate, cancellationToken);

        if (canModerate)
        {
            return post;
        }

        throw new NotFoundException(nameof(Post), postId);
    }
}
