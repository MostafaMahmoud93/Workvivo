using Microsoft.EntityFrameworkCore;
using Workvivo.Domain.Abstractions.Interfaces;
using Workvivo.Domain.Entities.Organization;
using Workvivo.Domain.Exceptions;

namespace Workvivo.Application.Common.Security;

/// <summary>
/// Resolves the signed-in account to the employee profile the rest of the platform
/// works in.
///
/// The two are deliberately separate - a login is a credential, an employee is a
/// person - so nearly every handler needs this translation. It was written inside
/// <c>PostAuthorization</c> first; pulling it out here stops each new feature area
/// growing its own slightly different copy, and gives one place to cache it per
/// request.
/// </summary>
public sealed class CurrentEmployee
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    private Guid? _resolved;

    public CurrentEmployee(IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>The caller's login id.</summary>
    public Guid UserId => _currentUser.UserId ?? throw new UnauthorizedException();

    /// <summary>
    /// The caller's employee id, or a failure if the account has no profile.
    ///
    /// Cached for the lifetime of the request: several handlers ask more than once,
    /// and this is scoped, so the second call is free.
    /// </summary>
    public async Task<Guid> RequireIdAsync(CancellationToken cancellationToken = default)
    {
        if (_resolved is { } cached)
        {
            return cached;
        }

        var userId = UserId;

        var employeeId = await _unitOfWork.Repository<Employee, Guid>()
            .GetAllQ()
            .Where(employee => employee.User_Id == userId)
            .Select(employee => (Guid?)employee.Id)
            .FirstOrDefaultAsync(cancellationToken);

        _resolved = employeeId ?? throw new ForbiddenException("You do not have an employee profile.");

        return _resolved.Value;
    }
}
