using Workvivo.Infrastructure.DBContext;

namespace Workvivo.Infrastructure.Repositories;
public class CustomBaseRepository<T> : ICustomBaseRepository<T> where T : class
{
    private readonly Workvivo_DbContext _context;
    public CustomBaseRepository(Workvivo_DbContext context)
    {
        _context = context;
    }
    #region Retraiv Data
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
    public async Task<List<T>> GetAllOrderingDescAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter = null)
    {
        try
        {
            if (predicateFilter == null)
            {
                return await _context.Set<T>().OrderByDescending(predicateSort).ToListAsync();
            }
            return await _context.Set<T>().OrderByDescending(predicateSort).Where(predicateFilter).ToListAsync();
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
    public async Task<List<T>> GetAllOrderingAscAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter = null)
    {
        try
        {
            if (predicateFilter == null)
            {
                return await _context.Set<T>().OrderBy(predicateSort).ToListAsync();
            }
            return await _context.Set<T>().OrderByDescending(predicateSort).Where(predicateFilter).ToListAsync();
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
    public async Task<T> FirstOrDefaultOrderingAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter)
    {
        try
        {
            return await _context.Set<T>().OrderByDescending(predicateSort).FirstOrDefaultAsync(predicateFilter);
        }
        catch (Exception ex)
        {
            throw ex;
        }
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
    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
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
            return await _context.Set<T>().ToListAsync();
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
            IQueryable<T> query = _context.Set<T>();
            return query;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    #endregion
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
    public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            return await _context.Set<T>().FirstOrDefaultAsync(predicate);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public virtual async Task<T> FindByIDAsync(Expression<Func<T, bool>> predicate = null)
    {

        return await _context.Set<T>().FirstOrDefaultAsync(predicate);
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
    public void DeleteById(int Id)
    {
        var entity = _context.Set<T>().Find(Id);
        _context.Set<T>().Remove(entity);
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
        //query = AddGlobalFilters(query);
        return query;
    }
    public Task<List<T>> ExecuteProc(string query, List<KeyValuePair<string, string>> parameters)
    {
        var sqlParameters = parameters.Select(a =>
        {
            return new SqlParameter(a.Key, a.Value);
        }).ToArray();
        return _context.Set<T>().FromSqlRaw(query, sqlParameters).ToListAsync();
    }
}
