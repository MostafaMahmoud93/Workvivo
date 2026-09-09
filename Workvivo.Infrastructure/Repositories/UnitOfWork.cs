using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;

namespace Workvivo.Infrastructure.Repositories;
public class UnitOfWork : IUnitOfWork
{
    private readonly Workvivo_DbContext _dbContext;
    private readonly Dictionary<Type, object> _repositories = [];
    private IDbContextTransaction? _currentTransaction;
    private readonly RoleManager<UserGroup> _userGroupManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userRepository;
    public UnitOfWork(Workvivo_DbContext dbContext, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<UserGroup> userGroupManager)
    {
        _dbContext = dbContext;
        _userRepository = userManager;
        _signInManager = signInManager;
        _userGroupManager = userGroupManager;
    }
    #region Repositories
    public IBaseRepository<GlobalAttachment, Guid> GlobalAttachmentRepository => new BaseRepository<GlobalAttachment, Guid>(_dbContext);
    public IBaseRepository<GroupPermissions, Guid> GroupPermissionsRepository => new BaseRepository<GroupPermissions, Guid>(_dbContext);
    public IBaseRepository<NotificationUser, Guid> NotificationUserRepository => new BaseRepository<NotificationUser, Guid>(_dbContext);
    public IBaseRepository<LinkScreenAction, Guid> LinkScreenActionRepository => new BaseRepository<LinkScreenAction, Guid>(_dbContext);
    public IBaseRepository<EmailSMSTemplate, int> EmailSMSTemplateRepository => new BaseRepository<EmailSMSTemplate, int>(_dbContext);
    public IBaseRepository<UserPermissions, Guid> UserScreenActionRepository => new BaseRepository<UserPermissions, Guid>(_dbContext);
    public IBaseRepository<UsersShortCuts, int> UsersShortCutsRepository => new BaseRepository<UsersShortCuts, int>(_dbContext);
    public IBaseRepository<LookupCategory, int> LookupCategoryRepository => new BaseRepository<LookupCategory, int>(_dbContext);
    public IBaseRepository<Notification, Guid> NotificationRepository => new BaseRepository<Notification, Guid>(_dbContext);
    public IBaseRepository<UserLoginLog, Guid> UserLoginLogRepository => new BaseRepository<UserLoginLog, Guid>(_dbContext);
    public IBaseRepository<ScreenAction, Guid> ScreenActionRepository => new BaseRepository<ScreenAction, Guid>(_dbContext);
    public IBaseRepository<EmailSMSHistory, Guid> EmailSMSHistory => new BaseRepository<EmailSMSHistory, Guid>(_dbContext);
    public ICustomBaseRepository<VW_UserActions> VWUserActions => new CustomBaseRepository<VW_UserActions>(_dbContext);
    public IBaseRepository<MainModule, Guid> MainModuleRepository => new BaseRepository<MainModule, Guid>(_dbContext);
    public IBaseRepository<SysSetting, Guid> SysSettingRepository => new BaseRepository<SysSetting, Guid>(_dbContext);
    public IBaseRepository<AccessLog, int> AccessLogRepository => new BaseRepository<AccessLog, int>(_dbContext);
    public IBaseRepository<Screen, Guid> ScreenRepository => new BaseRepository<Screen, Guid>(_dbContext);
    public IBaseRepository<MasterData, int> MasterData => new BaseRepository<MasterData, int>(_dbContext);
    public IBaseRepository<Lookup, int> LookupRepository => new BaseRepository<Lookup, int>(_dbContext);
    public SignInManager<ApplicationUser> SignInManager => _signInManager;
    public RoleManager<UserGroup> UserGroupManager => _userGroupManager;
    public UserManager<ApplicationUser> UserManager => _userRepository;
    #endregion
    public async Task<string> ExecuteSqlQueryAsync(string sqlQuery, params object?[] parameters)
    {
        try
        {
            var results = new List<Dictionary<string, object>>();

            // Create and configure the command
            await using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sqlQuery;
                command.CommandType = System.Data.CommandType.Text;

                // Add parameters if any
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@p{i}";
                    parameter.Value = parameters[i] ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }

                _dbContext.Database.OpenConnection();

                // Execute the query
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    // Get column names
                    var columnNames = Enumerable.Range(0, reader.FieldCount)
                                                .Select(reader.GetName)
                                                .ToList();

                    // Read each row and create a dictionary for it
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < columnNames.Count; i++)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            row[columnNames[i]] = value;
                        }
                        results.Add(row);
                    }
                }
            }

            // Serialize results to JSON
            var jsonResult = JsonSerializer.Serialize(results, new JsonSerializerOptions
            {
                WriteIndented = false, // Set to true if you want pretty-printing (indented)
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Optional: use camel case
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Allows non-escaped Unicode characters
            });

            return jsonResult;
        }
        catch (Exception ex)
        {
            // Handle exceptions (logging or rethrowing)
            _ = await CustomLogErrorAsync<string>(ex, null, new { sqlQuery, parameters });
            throw new Exception("An error occurred while executing the SQL query.", ex);
        }
    }

    /// <summary>
    /// Resolves - and caches - a repository for the requested entity.
    ///
    /// Cached because the named properties above construct a new BaseRepository on
    /// every access. That is harmless for a handful of them, but a handler that touches
    /// one entity in a loop would otherwise allocate a repository per iteration.
    /// </summary>
    public IBaseRepository<T, TKey> Repository<T, TKey>() where T : BaseCommonEntity<TKey>
    {
        var key = typeof(T);

        if (!_repositories.TryGetValue(key, out var repository))
        {
            repository = new BaseRepository<T, TKey>(_dbContext);
            _repositories[key] = repository;
        }

        return (IBaseRepository<T, TKey>)repository;
    }

    public async Task<int> SaveChangesAsync() => await _dbContext.SaveChangesAsync();
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        await _dbContext.SaveChangesAsync(cancellationToken);

    #region Transactions

    public bool HasActiveTransaction => _currentTransaction is not null;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            // Nesting is a caller bug, but throwing here would turn it into a 500 for
            // an end user. Reusing the open transaction gives the same semantics as
            // the nesting the caller expected.
            return;
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            // The tracker still holds the entities the rolled-back statements wrote.
            // Leaving them would make the next SaveChanges on this scoped context try
            // to persist changes the caller just abandoned.
            _dbContext.ChangeTracker.Clear();
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    #endregion

    public void Dispose()
    {
        _repositories.Clear();
        _currentTransaction?.Dispose();
        _currentTransaction = null;
        _dbContext.Dispose();
    }

    public void ClearTracker() => _dbContext.ChangeTracker.Clear();
    public void RejectChanges()
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries()
              .Where(e => e.State != EntityState.Unchanged))
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
                case EntityState.Modified:
                case EntityState.Deleted:
                    entry.Reload();
                    break;
            }
        }
    }
    private async Task<T> CustomLogErrorAsync<T>(Exception ex, T data, object inputs)
    {
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString(@"yyyy\\MM"))))
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString(@"yyyy\\MM")));

        string path = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString(@"yyyy\\MM\\dd")) + ".log";
        string contents = $@"==={DateTime.Now.ToString("HH:mm:ss")}=======================================================================================
                            {ex.Message}
                            ----------------------------------------
                            {ex.InnerException?.Message}
                            ----------------------------------------
                            {ex.StackTrace}
                            ----------------------------------------
                            {Newtonsoft.Json.JsonConvert.SerializeObject(inputs)}
                            ==================================================================================================" + Environment.NewLine;
        await File.AppendAllTextAsync(path, contents);
        return data;
    }
}
