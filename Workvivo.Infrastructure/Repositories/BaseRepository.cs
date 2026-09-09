using Workvivo.Infrastructure.DBContext;

namespace Workvivo.Infrastructure.Repositories;
public class BaseRepository<T, U> : IBaseRepository<T, U> where T : BaseCommonEntity<U>
{
    private readonly Workvivo_DbContext _context;
    public BaseRepository(Workvivo_DbContext context)
    {
        _context = context;
    }
    private IQueryable<T> CommonContext()
    {
        try
        {
            return _context.Set<T>().Where(a => !a.Is_Deleted);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<List<T>> GetAllAsync(bool withTracking = true)
    {
        try
        {
            if (withTracking)
                return await CommonContext().ToListAsync();
            else
                return await CommonContext().AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<List<T>> GetAllAsNoTrackingAsync()
    {
        try
        {

            return await CommonContext().AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<List<T>> GetAllAsync()
    {
        try
        {

            return await CommonContext().ToListAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<int> GetSerialNoAsync()
    {
        try
        {
            return await _context.Set<T>().CountAsync() + 1;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<int> GetSerialNoAsync(Expression<Func<T, int>> result)
    {
        try
        {
            var maxSerialNo = await _context.Set<T>().MaxAsync(result);

            return maxSerialNo + 1;
        }
        catch (Exception ex)
        {
            //throw ex;
            return 1;
        }
    }
    public async Task<int> GetSerialNoWithFilterAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            return await _context.Set<T>().Where(predicate).CountAsync() + 1;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public IQueryable<T> GetAllQ()
    {
        try
        {
            IQueryable<T> query = CommonContext();
            return query;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            return await CommonContext().Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public IEnumerable<IGrouping<TKey, T>> GetAllGroupBy<TKey>(Func<T, TKey> keySelector, Expression<Func<T, bool>> predicate)
    {
        return CommonContext().Where(predicate).GroupBy(keySelector);
    }
    public IEnumerable<TResult> GetAllGroupBy<TKey, TResult>(Func<T, TKey> keySelector, Expression<Func<T, bool>> predicate, Func<IGrouping<TKey, T>, TResult> resultSelector)
    {
        return CommonContext()
            .Where(predicate)                  // Apply filtering
            .GroupBy(keySelector)               // Group by key (e.g., YEAR)
            .Select(resultSelector);            // Project into TResult
    }
    public async Task<List<Tkey>> GetAllWithSelectAsync<Tkey>(Expression<Func<T, bool>> predicate, Expression<Func<T, Tkey>> keySelector)
    {
        try
        {
            return await CommonContext().Where(predicate).Select(keySelector).ToListAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<List<Tkey>> GetAllWithSelectAsync<Tkey>(Expression<Func<T, bool>> predicate, Expression<Func<T, Tkey>> keySelector, params Expression<Func<T, object>>[] includes)
    {
        try
        {
            return await InsializeQuery(includes).Where(a => !a.Is_Deleted).Where(predicate).Select(keySelector).ToListAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<TStatistics> GetStatisticsAsync<TStatistics>(Expression<Func<T, bool>>? filter = null, List<(string propertyName, Expression<Func<T, bool>> condition)>? conditions = null) where TStatistics : class, new()
    {
        // Prepare the base query
        var query = _context.Set<T>().AsQueryable();

        // Apply the optional filter
        if (filter != null)
        {
            query = query.Where(filter);
        }

        // Initialize the statistics model
        var statistics = new TStatistics();

        // Ensure conditions are provided
        if (conditions != null)
        {
            foreach (var (propertyName, condition) in conditions)
            {
                var property = typeof(TStatistics).GetProperty(propertyName);
                if (property != null && property.PropertyType == typeof(int))
                {
                    // Calculate and assign the count
                    var count = await query.CountAsync(condition);
                    property.SetValue(statistics, count);
                }
            }
        }

        return statistics;
    }
    public async Task<TValue> SelectFirstPropertyValue<TValue>(Expression<Func<T, bool>> predicate, string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        //var property = typeof(T).GetType().GetProperties().FirstOrDefault(p => p.Name.Replace("_", "").ToLower().Equals(propertyName.ToLower()));

        if (property == null)
        {
            throw new ArgumentException($"Property '{propertyName}' does not exist in type '{typeof(T).Name}'.");
        }

        var entity = await _context.Set<T>().Where(predicate).FirstOrDefaultAsync();

        if (entity == null)
        {
            throw new InvalidOperationException("No entities in the collection.");
        }

        var value = (TValue)property.GetValue(entity);
        return value;
    }
    public async Task<TValue> SelectFirstPropertyValue<TValue, TKey>(Expression<Func<T, bool>> predicate, Expression<Func<T, TKey>> keySelector)
    {
        var memberExpression = keySelector.Body as MemberExpression;

        if (memberExpression == null)
        {
            throw new ArgumentException("Invalid key selector. It must be a property selector.", nameof(keySelector));
        }

        var property = memberExpression.Member as PropertyInfo;

        if (property == null)
        {
            throw new ArgumentException("Invalid key selector. It must be a property selector.", nameof(keySelector));
        }

        var entity = await _context.Set<T>().Where(predicate).FirstOrDefaultAsync();

        if (entity == null)
        {
            throw new InvalidOperationException("No entities in the collection.");
        }

        var value = (TValue)property.GetValue(entity);
        return value;
    }
    public async Task<int> ExecuteUpdateAsync(Expression<Func<T, bool>> predicate, Action<UpdateSettersBuilder<T>> setters, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().Where(predicate).ExecuteUpdateAsync(setters, cancellationToken);
    }
    public async Task<List<T>> GetAllPaginationAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize)
    {
        try
        {
            return await _context.Set<T>().Skip((pageNumber - 1) * pageSize).Take(pageSize).Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<List<T>> GetAllOrderingDescAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter = null)
    {
        try
        {
            if (predicateFilter == null)
            {
                return await CommonContext().OrderByDescending(predicateSort).ToListAsync();
            }
            return await CommonContext().OrderByDescending(predicateSort).Where(predicateFilter).ToListAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<List<T>> GetAllOrderingAscAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter = null)
    {
        try
        {
            if (predicateFilter == null)
            {
                return await CommonContext().OrderBy(predicateSort).ToListAsync();
            }
            return await CommonContext().OrderBy(predicateSort).Where(predicateFilter).ToListAsync();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<List<T>> GetAllOrderingDescWithIncludesAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter = null, params Expression<Func<T, object>>[] includes)
    {
        try
        {
            if (predicateFilter == null)
            {
                return await InsializeQuery(includes).OrderByDescending(predicateSort).ToListAsync();
            }
            return await InsializeQuery(includes).OrderByDescending(predicateSort).Where(predicateFilter).ToListAsync();

        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<List<T>> GetAllWithIncludesAsync(Expression<Func<T, bool>> predicateFilter = null, params Expression<Func<T, object>>[] includes)
    {
        try
        {
            if (predicateFilter == null)
            {
                return await InsializeQuery(includes).ToListAsync();
            }
            return await InsializeQuery(includes).Where(predicateFilter).ToListAsync();

        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<T> FirstOrDefaultOrderingWithIncludesAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter, params Expression<Func<T, object>>[] includes)
    {
        try
        {
            return await InsializeQuery(includes).OrderByDescending(predicateSort).FirstOrDefaultAsync(predicateFilter);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<T> FirstOrDefaultWithIncludesAsync(Expression<Func<T, bool>> predicateFilter, params Expression<Func<T, object>>[] includes)
    {
        try
        {
            return await InsializeQuery(includes).FirstOrDefaultAsync(predicateFilter);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            return await CommonContext().FirstOrDefaultAsync(predicate);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public virtual async Task<T> FindByIDAsync(Expression<Func<T, bool>> predicate = null)
    {

        return await CommonContext().FirstOrDefaultAsync(predicate);
    }
    public virtual async Task<T> FindByIDAsync(U Id)
    {
        return await CommonContext().FirstOrDefaultAsync(z => z.Id.Equals(Id) && z.Is_Deleted == false);
    }
    public async Task<T> FirstOrDefaultOrderingAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter)
    {
        try
        {
            return await CommonContext().OrderByDescending(predicateSort).FirstOrDefaultAsync(predicateFilter);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task AddAsync(T entity)
    {
        try
        {
            await _context.Set<T>().AddAsync(entity);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task AddRangeAsync(List<T> entity)
    {
        try
        {
            await _context.Set<T>().AddRangeAsync(entity);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public bool Any(Func<T, bool> predicate)
    {
        return CommonContext().Any(predicate);
    }
    public void DeleteById(int Id)
    {
        var entity = _context.Set<T>().Find(Id);
        _context.Set<T>().Remove(entity);
    }
    public void SoftDeleteById(U Id)
    {
        var entity = _context.Set<T>().Find(Id);
        entity.Is_Deleted = true;
    }
    public void DeleteByEntity(T entity)
    {
        _context.Set<T>().Remove(entity);
    }
    public void Edit(T entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
    }
    public R Max<R>(Expression<Func<T, R>> result, Expression<Func<T, bool>> filter)
    {
        try
        {
            var query = _context.Set<T>().AsQueryable();
            if (filter != null)
                query = query.Where(filter);

            if (typeof(R) == typeof(Nullable<>))
            {
                query = query.Where(x => x != null);
            }
            var outcome = query.Max(result);
            return outcome;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public decimal Sum(Expression<Func<T, decimal>> result, Expression<Func<T, bool>> filter)
    {
        try
        {
            var query = _context.Set<T>().AsQueryable();
            if (filter != null)
                query = query.Where(filter);
            var outcome = query.Sum(result);
            return outcome;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public int Sum(Expression<Func<T, int>> result, Expression<Func<T, bool>> filter)
    {
        try
        {
            var query = _context.Set<T>().AsQueryable();
            if (filter != null)
                query = query.Where(filter);
            var outcome = query.Sum(result);
            return outcome;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public void DeleteRange(Expression<Func<T, bool>> predicate)
    {
        var list = _context.Set<T>().Where(predicate);

        _context.Set<T>().RemoveRange(list);
    }
    public async Task Delete(Expression<Func<T, bool>> predicate)
    {
        var list = await _context.Set<T>().FirstOrDefaultAsync(predicate);
        if (list != null)
        {
            _context.Set<T>().Remove(list);
        }
    }
    public async Task<List<T>> GetAllWhereAsync<T>(Expression<Func<T, bool>> predicate, bool withTracking = true) where T : class
    {
        if (withTracking)
            return await InsializeQuery<T>().Where(predicate).ToListAsync();
        else
            return await InsializeQuery<T>().Where(predicate).AsNoTracking().ToListAsync();
    }
    public async Task<(List<T> collection, int length)> GetPagedAndSortedWithFilterAsync(int page = 1, int pageSize = 10, Expression<Func<T, bool>> predicate = null, string? sortBy = null, bool sortAsc = true) // الحمد لله والشكر لله
    {
        var query = _context.Set<T>().AsQueryable();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        int count = query.Count();
        #region Sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            PropertyInfo property = typeof(T).GetProperty(sortBy);

            if (property != null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T), "x");

                Expression propertyAccess = Expression.Property(parameter, sortBy);

                LambdaExpression orderByExpression = Expression.Lambda(propertyAccess, parameter);

                MethodCallExpression orderByCallExpression = Expression.Call(
                    typeof(Queryable),
                sortAsc ? "OrderBy" : "OrderByDescending",
                    new Type[] { typeof(T), property.PropertyType },
                    query.Expression,
                    Expression.Quote(orderByExpression));
                query = query.Provider.CreateQuery<T>(orderByCallExpression);

            }
        }
        #endregion
        var skip = (page - 1) * pageSize;
        return (await query.Skip(skip).Take(pageSize).ToListAsync(), count);
    }
    public async Task<(List<T> collection, int length)> GetPagedAndSortedWithFilterAndIncludeAsync(int page = 1, int pageSize = 10, Expression<Func<T, bool>> predicate = null, string? sortBy = null, bool sortAsc = true, params Expression<Func<T, object>>[] includes)
    {
        var query = InsializeQuery(includes);
        int count = query.Count();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        count = query.Count();
        #region Sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            PropertyInfo property = typeof(T).GetProperty(sortBy);

            if (property != null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T), "x");

                Expression propertyAccess = Expression.Property(parameter, sortBy);

                LambdaExpression orderByExpression = Expression.Lambda(propertyAccess, parameter);

                MethodCallExpression orderByCallExpression = Expression.Call(
                    typeof(Queryable),
                sortAsc ? "OrderBy" : "OrderByDescending",
                    new Type[] { typeof(T), property.PropertyType },
                    query.Expression,
                    Expression.Quote(orderByExpression));
                query = query.Provider.CreateQuery<T>(orderByCallExpression);

            }
        }
        #endregion
        var skip = (page - 1) * pageSize;
        return (await query.Skip(skip).Take(pageSize).ToListAsync(), count);
    }
    public async Task<(List<T> collection, int length)> GetPagedAndSortedWithGeneralFilterAndIncludeAsync(int page = 1, int pageSize = 10, Expression<Func<T, bool>> predicate = null, string? filter = null, string? sortBy = null, bool sortAsc = true, params Expression<Func<T, object>>[] includes)
    {
        // Initialize the query with includes
        var query = InsializeQuery(includes);

        // Apply the initial predicate if provided
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        // Apply the filter if provided
        if (!string.IsNullOrEmpty(filter))
        {
            // Get all string properties of the entity type
            var stringProperties = typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(string));

            // Build the predicate dynamically
            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? filterExpression = null;

            foreach (var property in stringProperties)
            {
                var propertyExpression = Expression.Property(parameter, property.Name);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

                if (containsMethod != null)
                {
                    var searchExpression = Expression.Call(propertyExpression, containsMethod, Expression.Constant(filter, typeof(string)));
                    filterExpression = filterExpression == null ? searchExpression : Expression.OrElse(filterExpression, searchExpression);
                }
            }

            // If a filterExpression was constructed, apply it
            if (filterExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(filterExpression, parameter);
                query = query.Where(lambda);
            }
        }

        // Count after applying all filters
        int count = query.Count();

        // Apply sorting if specified
        if (!string.IsNullOrEmpty(sortBy))
        {
            PropertyInfo? property = typeof(T).GetProperty(sortBy);

            if (property != null)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var propertyAccess = Expression.Property(parameter, sortBy);
                var orderByExpression = Expression.Lambda(propertyAccess, parameter);

                var methodName = sortAsc ? "OrderBy" : "OrderByDescending";
                var orderByCallExpression = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new Type[] { typeof(T), property.PropertyType },
                    query.Expression,
                    Expression.Quote(orderByExpression));

                query = query.Provider.CreateQuery<T>(orderByCallExpression);
            }
        }

        // Pagination
        var skip = (page - 1) * pageSize;
        var result = await query.Skip(skip).Take(pageSize).ToListAsync();

        return (result, count);
    }
    public async Task<(List<T> collection, int length)> GetPagedAndSortedWithPivotAsync<T, TRelated>(int page = 1, int pageSize = 10, Expression<Func<T, bool>> predicate = null, string? sortBy = null, bool sortAsc = true, string? filter = null, Expression<Func<T, IEnumerable<TRelated>>> relatedSelector = null, Expression<Func<TRelated, object>> pivotSelector = null, Expression<Func<TRelated, bool>> pivotCondition = null) where T : class where TRelated : class
    {
        var query = _context.Set<T>().AsQueryable();

        // Apply initial filter if predicate is not null
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        // Apply string filter if provided
        if (!string.IsNullOrEmpty(filter))
        {
            var parameter = Expression.Parameter(typeof(T), "x");

            // Filter based on header (T)
            var headerFilter = BuildStringContainsExpression<T>(parameter, filter);

            // Filter based on details (TRelated)
            Expression<Func<T, bool>> detailFilter = null;
            if (relatedSelector != null)
            {
                var relatedCollection = Expression.Invoke(relatedSelector, parameter);

                var anyCall = Expression.Call(
                    typeof(Enumerable),
                    "Any",
                    new Type[] { typeof(TRelated) },
                    relatedCollection,
                    BuildStringContainsExpression<TRelated>(Expression.Parameter(typeof(TRelated), "r"), filter)
                );

                detailFilter = Expression.Lambda<Func<T, bool>>(anyCall, parameter);
            }

            // Combine header and detail filters with OR
            if (detailFilter != null)
            {
                var combinedFilter = Expression.Lambda<Func<T, bool>>(
                    Expression.OrElse(headerFilter.Body, detailFilter.Body),
                    parameter
                );
                query = query.Where(combinedFilter);
            }
            else
            {
                query = query.Where(headerFilter);
            }
        }

        // Include related data if relatedSelector is provided
        if (relatedSelector != null)
        {
            query = query.Include(relatedSelector);
        }

        // Count the total number of records before paging
        int count = await query.CountAsync();

        // Apply sorting by header or pivot
        if (!string.IsNullOrEmpty(sortBy))
        {
            var property = typeof(T).GetProperty(sortBy);

            if (property != null)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var propertyAccess = Expression.Property(parameter, sortBy);
                var orderByExpression = Expression.Lambda(propertyAccess, parameter);

                var orderByCallExpression = Expression.Call(
                    typeof(Queryable),
                    sortAsc ? "OrderBy" : "OrderByDescending",
                    new Type[] { typeof(T), property.PropertyType },
                    query.Expression,
                    Expression.Quote(orderByExpression)
                );

                query = query.Provider.CreateQuery<T>(orderByCallExpression);
            }
            else if (relatedSelector != null && pivotSelector != null)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var relatedCollection = Expression.Invoke(relatedSelector, parameter);

                var whereCall = Expression.Call(
                    typeof(Enumerable),
                    "Where",
                    new Type[] { typeof(TRelated) },
                    relatedCollection,
                    pivotCondition ?? Expression.Lambda<Func<TRelated, bool>>(Expression.Constant(true), Expression.Parameter(typeof(TRelated), "p"))
                );

                var selectCall = Expression.Call(
                    typeof(Enumerable),
                    "Select",
                    new Type[] { typeof(TRelated), typeof(object) },
                    whereCall,
                    pivotSelector
                );

                var firstOrDefaultCall = Expression.Call(
                    typeof(Enumerable),
                    "FirstOrDefault",
                    new Type[] { typeof(object) },
                    selectCall
                );

                var orderByExpression = Expression.Lambda(firstOrDefaultCall, parameter);

                var orderByCallExpression = Expression.Call(
                    typeof(Queryable),
                    sortAsc ? "OrderBy" : "OrderByDescending",
                    new Type[] { typeof(T), typeof(object) },
                    query.Expression,
                    Expression.Quote(orderByExpression)
                );

                query = query.Provider.CreateQuery<T>(orderByCallExpression);
            }
        }

        // Apply paging to the sorted query
        var skip = (page - 1) * pageSize;
        var pagedData = await query.Skip(skip).Take(pageSize).ToListAsync();

        return (pagedData, count);
    }
    //public bool HasPopulatedCollectionsUsed(T entity)
    //{
    //    PropertyInfo[] properties = typeof(T).GetProperties();
    //    foreach (PropertyInfo property in properties)
    //    {
    //        if (property.PropertyType.IsGenericType &&
    //            property.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
    //        {
    //            //PropertyInfo property = typeof(Entity).GetProperty("CollectionProperty");
    //            object value = property.GetValue(entity);
    //            ICollection<object> collection2 = value as ICollection<object>;
    //            var collection = (ICollection<object>)property.GetValue(entity);
    //            if (collection != null && collection.Any())
    //            {
    //                return true;
    //            }
    //        }
    //    }
    //    return false;
    //}
    public bool HasPopulatedCollectionsUsed(T entity)
    {
        PropertyInfo[] properties = typeof(T).GetProperties();
        foreach (PropertyInfo property in properties)
        {
            if (IsCollectionProperty(property))
            {
                IEnumerable collection = (IEnumerable)property.GetValue(entity);
                if (collection != null && collection.Cast<object>().Any())
                {
                    return true;
                }
            }
        }
        return false;
    }
    //public bool HasPopulatedCollectionsUsed(T entity)
    //{
    //    PropertyInfo[] properties = typeof(T).GetProperties();

    //    foreach (PropertyInfo property in properties)
    //    {
    //        if (IsCollectionProperty(property))
    //        {
    //            ICollection collection = (ICollection)property.GetValue(entity);
    //            if (collection != null && collection.Count > 0)
    //            {
    //                return true;
    //            }
    //        }
    //    }

    //    return false;
    //}
    private bool IsCollectionProperty(PropertyInfo property)
    {
        return property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>);
    }
    #region Private logic
    private IQueryable<T> InsializeQuery<T>(params Expression<Func<T, object>>[] includes) where T : class
    {
        var query = _context.Set<T>().AsQueryable();
        if (includes.Any())
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }
        return query;
    }
    private IQueryable<T> InsializeQuery(params Expression<Func<T, object>>[] includes)
    {
        var query = _context.Set<T>().AsQueryable();
        if (includes.Any())
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }
        return query;
    }
    private Expression<Func<T, bool>> BuildStringContainsExpression<T>(ParameterExpression parameter, string filter)
    {
        var properties = typeof(T).GetProperties().Where(p => p.PropertyType == typeof(string));
        Expression expression = null;

        foreach (var property in properties)
        {
            var propertyAccess = Expression.Property(parameter, property);
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

            var containsExpression = Expression.Call(propertyAccess, containsMethod, Expression.Constant(filter));

            expression = expression == null ? containsExpression : Expression.OrElse(expression, containsExpression);
        }

        if (expression == null)
        {
            return _ => false; // No string properties to filter on, return a false condition
        }

        return Expression.Lambda<Func<T, bool>>(expression, parameter);
    }
    #endregion
}
