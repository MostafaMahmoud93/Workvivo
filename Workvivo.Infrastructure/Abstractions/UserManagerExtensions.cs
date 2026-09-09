namespace Workvivo.Infrastructure.Abstractions
{
    public static class UserManagerExtensions
    {
        public static async Task<(List<ApplicationUser> collection, int length)> GetPagedAndSortedWithFilterAsync(
            this UserManager<ApplicationUser> userManager,
            int page = 1,
            int pageSize = 10,
            Expression<Func<ApplicationUser, bool>> predicate = null,
            string? sortBy = null,
            bool sortAsc = true)
        {
            // Start with the Users queryable
            var query = userManager.Users.AsQueryable();

            // Apply filtering
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Count total users after filtering
            int count = await query.CountAsync();

            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                PropertyInfo property = typeof(ApplicationUser).GetProperty(sortBy);

                if (property != null)
                {
                    ParameterExpression parameter = Expression.Parameter(typeof(ApplicationUser), "x");
                    Expression propertyAccess = Expression.Property(parameter, sortBy);
                    LambdaExpression orderByExpression = Expression.Lambda(propertyAccess, parameter);

                    MethodCallExpression orderByCallExpression = Expression.Call(
                        typeof(Queryable),
                        sortAsc ? "OrderBy" : "OrderByDescending",
                        new Type[] { typeof(ApplicationUser), property.PropertyType },
                        query.Expression,
                        Expression.Quote(orderByExpression));

                    query = query.Provider.CreateQuery<ApplicationUser>(orderByCallExpression);
                }
            }

            // Apply pagination
            var skip = (page - 1) * pageSize;
            var users = await query.Skip(skip).Take(pageSize).ToListAsync();

            return (users, count);
        }
    }
}
