namespace Workvivo.Domain.Abstractions.Interfaces;
public interface ICustomBaseRepository<T> where T : class
{
    Task<List<T>> GetAllOrderingDescWithIncludesAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter = null, params Expression<Func<T, object>>[] includes);
    Task<(List<T> collection, int length)> GetPagedAndSortedWithFilterAsync(int page = 1, int pageSize = 10, Expression<Func<T, bool>> predicate = null, string? sortBy = null, bool sortAsc = true);
    Task<T> FirstOrDefaultOrderingWithIncludesAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter, params Expression<Func<T, object>>[] includes);
    Task<List<T>> GetAllOrderingDescAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter = null);
    Task<List<T>> GetAllOrderingAscAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter = null);
    Task<List<T>> GetAllWithIncludesAsync(Expression<Func<T, bool>> predicateFilter = null, params Expression<Func<T, object>>[] includes);
    Task<T> FirstOrDefaultWithIncludesAsync(Expression<Func<T, bool>> predicateFilter, params Expression<Func<T, object>>[] includes);
    Task<T> FirstOrDefaultOrderingAsync<Tkey>(Expression<Func<T, Tkey>> predicateSort, Expression<Func<T, bool>> predicateFilter);
    Task<List<T>> GetAllWhereAsync<T>(Expression<Func<T, bool>> predicate, bool withTracking = true) where T : class;
    Task<List<T>> GetAllPaginationAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize);
    Task<List<T>> ExecuteProc(string query, List<KeyValuePair<string, string>> parameters);
    decimal Sum(Expression<Func<T, decimal>> result, Expression<Func<T, bool>> filter);
    int Sum(Expression<Func<T, int>> result, Expression<Func<T, bool>> filter);
    R Max<R>(Expression<Func<T, R>> result, Expression<Func<T, bool>> filter);
    Task<T> FindByIDAsync(Expression<Func<T, bool>> predicate = null);
    Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate);
    void DeleteRange(Expression<Func<T, bool>> predicate);
    Task Delete(Expression<Func<T, bool>> predicate);
    Task AddRangeAsync(List<T> entity);
    void DeleteByEntity(T entity);
    Task<int> GetSerialNoAsync();
    Task<List<T>> GetAllAsync();
    void DeleteById(int Id);
    Task AddAsync(T entity);
    IQueryable<T> GetAllQ();
    void Edit(T entity);
}
