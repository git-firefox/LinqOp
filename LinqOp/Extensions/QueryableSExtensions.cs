using System.Linq.Expressions;
using LinqOp.Models;
using Microsoft.EntityFrameworkCore;

namespace LinqOp.Extensions;

public static class QueryableSExtensions
{
    // Extension method to count words in a string
    public static async Task<DataSourceResult<TResult>> ToDataSourceResultAsync<TResult>(this IQueryable<TResult> query, DataSourceRequest request, CancellationToken cancellationToken = default)
        where TResult : class
    {
        // 🔹 Apply Filters
        foreach (var filter in request.Filters)
        {
            var param = Expression.Parameter(typeof(TResult), "x");
            var property = Expression.Property(param, filter.Member);
            var constant = Expression.Constant(filter.Value);

            Expression body;

            switch (filter.Operator)
            {
                case "contains":
                    body = Expression.Call(property, "Contains", null, constant);
                    break;
                case "startswith":
                    body = Expression.Call(property, "StartsWith", null, constant);
                    break;
                case "endswith":
                    body = Expression.Call(property, "EndsWith", null, constant);
                    break;
                default: // "eq"
                    body = Expression.Equal(property, constant);
                    break;
            }

            var lambda = Expression.Lambda<Func<TResult, bool>>(body, param);
            query = query.Where(lambda);
        }

        // 🔹 Total count (before paging)
        int total = await query.CountAsync(cancellationToken);

        // 🔹 Apply Sorting
        IOrderedQueryable<TResult>? orderedQuery = null;
        for (int i = 0; i < request.Sorts.Count; i++)
        {
            var sort = request.Sorts[i];
            if (i == 0)
            {
                orderedQuery = sort.Descending
                      ? query.OrderByDescending(x => EF.Property<object>(x, sort.Member))
                      : query.OrderBy(x => EF.Property<object>(x,    sort.Member));
            }
            else
            {
                orderedQuery = sort.Descending
                    ? orderedQuery!.ThenByDescending(x => EF.Property<object>(x, sort.Member))
                    : orderedQuery!.ThenBy(x => EF.Property<object>(x, sort.Member));
            }
        }

        if (orderedQuery != null)
            query = orderedQuery;

        // 🔹 Paging
        var items = await query.Skip(request.Skip).Take(request.Take).ToListAsync(cancellationToken);

        return new(items, total);
    }
}
