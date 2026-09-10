using Workvivo.Domain.Entities.BaseEntities;
using Workvivo.Domain.Entities.Common;
using Workvivo.Domain.Entities.Identity;
using Workvivo.Domain.Entities.RealTime;
using Workvivo.Domain.Entities.Views;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Workvivo.Domain.Abstractions.Interfaces;
public interface IUnitOfWork
{
    #region Repositories
    IBaseRepository<LinkScreenAction, Guid> LinkScreenActionRepository { get; }
    IBaseRepository<GroupPermissions, Guid> GroupPermissionsRepository { get; }
    IBaseRepository<NotificationUser, Guid> NotificationUserRepository { get; }
    IBaseRepository<GlobalAttachment, Guid> GlobalAttachmentRepository { get; }
    IBaseRepository<EmailSMSTemplate, int> EmailSMSTemplateRepository { get; }
    IBaseRepository<UserPermissions, Guid> UserScreenActionRepository { get; }
    IBaseRepository<UsersShortCuts, int> UsersShortCutsRepository { get; }
    IBaseRepository<LookupCategory, int> LookupCategoryRepository { get; }
    IBaseRepository<Notification, Guid> NotificationRepository { get; }
    IBaseRepository<ScreenAction, Guid> ScreenActionRepository { get; }
    IBaseRepository<UserLoginLog, Guid> UserLoginLogRepository { get; }
    IBaseRepository<MainModule, Guid> MainModuleRepository { get; }
    IBaseRepository<EmailSMSHistory, Guid> EmailSMSHistory { get; }
    IBaseRepository<SysSetting, Guid> SysSettingRepository { get; }
    IBaseRepository<AccessLog, int> AccessLogRepository { get; }
    ICustomBaseRepository<VW_UserActions> VWUserActions { get; }
    IBaseRepository<Screen, Guid> ScreenRepository { get; }
    IBaseRepository<Lookup, int> LookupRepository { get; }
    SignInManager<ApplicationUser> SignInManager { get; }
    IBaseRepository<MasterData, int> MasterData { get; }
    UserManager<ApplicationUser> UserManager { get; }
    RoleManager<UserGroup> UserGroupManager { get; }
    #endregion
    /// <summary>
    /// A repository for any entity, resolved on demand.
    ///
    /// The named properties above are the template's convention and are left alone,
    /// but the platform adds roughly fifty entities and listing every one here would
    /// turn this interface into a wall of near-identical lines that has to be edited
    /// for each new table. The generic accessor is equivalent and does not grow.
    ///
    /// Instances are cached per unit of work, so repeated calls within a request share
    /// one repository over the same change tracker.
    /// </summary>
    IBaseRepository<T, TKey> Repository<T, TKey>() where T : BaseCommonEntity<TKey>;

    Task<string> ExecuteSqlQueryAsync(string sqlQuery, params object?[] parameters);
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Cancellable save. The parameterless overload above is kept so the existing
    /// services keep compiling; new code should pass the request's token through so
    /// an abandoned request stops doing database work.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    #region Transactions

    /// <summary>
    /// True when a transaction opened through this unit of work is still in flight.
    /// TransactionBehavior checks it so a handler that manages its own transaction is
    /// not wrapped in a second one - nesting BeginTransaction on the same connection
    /// throws.
    /// </summary>
    bool HasActiveTransaction { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Flushes pending changes and commits. No-op when no transaction is open.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back and discards tracked changes. Safe to call when nothing is open.</summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    #endregion

    /// <summary>
    /// Takes every domain event raised during this unit of work and clears them from
    /// the entities that raised them.
    ///
    /// Draining rather than reading: the caller now owns the events, and a second call
    /// returns nothing. That is what stops the same "you were mentioned" notification
    /// being sent twice when a handler saves more than once.
    ///
    /// Nothing is dispatched here. Events are published after the transaction commits,
    /// because a notification or an email raised inside it survives a rollback and
    /// there is no way to take an email back.
    /// </summary>
    IReadOnlyList<IDomainEvent> DrainDomainEvents();

    void RejectChanges();
    void ClearTracker();
    void Dispose();
}
