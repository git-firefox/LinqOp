using System.Linq.Expressions;
using System.Reflection;
using LinqOp.Models;
using Microsoft.EntityFrameworkCore;

namespace LinqOp.Extensions;

public static class QueryableExtensions
{
    public static async Task<DataSourceResult<TResult>> ToDataSourceResultAsync<TResult>(
        this IQueryable<TResult> query,
        DataSourceRequest request,
        CancellationToken cancellationToken = default) where TResult : class
    {
        // Apply filtering
        if (request.Filters.Count != 0)
        {
            query = ApplyFilters(query, request.Filters);
        }

        // Get total count BEFORE paging
        var total = await query.CountAsync(cancellationToken);

        // Apply sorting
        if (request.Sorts.Count != 0)
        {
            query = ApplySorting(query, request.Sorts);
        }

        // Aggregates
        IDictionary<string, IDictionary<string, object>>? aggregates = null;
        if (request.Aggregates.Count != 0)
        {
            aggregates = await ApplyAggregates(query, request.Aggregates, cancellationToken);
        }

        // Apply paging
        if (request.Take <= 0)
        {
            request.Take = 10; // set default to 10
        }
        query = query.Skip(request.Skip).Take(request.Take);

        // Execute query
        var data = await query.ToListAsync(cancellationToken);

        return new DataSourceResult<TResult>(data, total, aggregates);
    }

    private static IQueryable<TResult> ApplyFilters<TResult>(IQueryable<TResult> query, IList<FilterDescriptor> filters)
    {
        foreach (var filter in filters)
        {
            var propertyInfo = typeof(TResult).GetProperty(filter.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null) continue;

            var parameter = Expression.Parameter(typeof(TResult), "x");
            var member = Expression.PropertyOrField(parameter, filter.Member);

            var body = LinqExtensionsHelpers.BuildFilterExpression(member, filter.Operator, filter.Value);

            if (body == null) continue;

            var lambda = Expression.Lambda<Func<TResult, bool>>(body, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    private static IQueryable<TResult> ApplySorting<TResult>(IQueryable<TResult> query, IList<SortDescriptor> sorts)
    {
        bool first = true;

        foreach (var sort in sorts)
        {
            var propertyInfo = typeof(TResult).GetProperty(sort.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null) continue;

            var parameter = Expression.Parameter(typeof(TResult), "x");
            var member = Expression.PropertyOrField(parameter, sort.Member);
            var lambda = Expression.Lambda(member, parameter);

            string method = first
                ? (sort.Dir == SortDirection.Asc ? "OrderBy" : "OrderByDescending")
                : (sort.Dir == SortDirection.Asc ? "ThenBy" : "ThenByDescending");

            query = query.Provider.CreateQuery<TResult>(
                Expression.Call(
                    typeof(Queryable),
                    method,
                    [typeof(TResult), member.Type],
                    query.Expression,
                    Expression.Quote(lambda)));

            first = false;
        }

        return query;
    }

    private static async Task<IDictionary<string, IDictionary<string, object>>> ApplyAggregates<TResult >(
            IQueryable<TResult> query,
            IList<AggregateDescriptor> aggregates,
            CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, IDictionary<string, object>>();

        foreach (var group in aggregates.GroupBy(a => a.Member))
        {
            var member = group.Key;
            var prop = typeof(TResult).GetProperty(member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) continue;

            var parameter = Expression.Parameter(typeof(TResult), "x");
            var property = Expression.PropertyOrField(parameter, member);
            var lambda = Expression.Lambda(property, parameter);
            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var aggResults = new Dictionary<string, object>();

            foreach (var agg in group)
            {
                if (!LinqExtensionsHelpers.IsAggregatable(targetType, agg.Aggregate))
                    continue;

                switch (agg.Aggregate)
                {
                    case AggregateFunction.Count:
                        aggResults["Count"] = await query.CountAsync(cancellationToken);
                        break;

                    case AggregateFunction.Sum:
                        var sumLambda = Expression.Lambda<Func<TResult, decimal?>>(Expression.Convert(property, typeof(decimal?)), parameter);
                        aggResults["Sum"] = await query.SumAsync(sumLambda, cancellationToken);
                        break;

                    case AggregateFunction.Min:
                        var minLambda = Expression.Lambda<Func<TResult, object>>(Expression.Convert(property, typeof(object)), parameter);
                        aggResults["Min"] = await query.MinAsync(minLambda, cancellationToken);
                        break;

                    case AggregateFunction.Max:
                        var maxLambda = Expression.Lambda<Func<TResult, object>>(Expression.Convert(property, typeof(object)), parameter);
                        aggResults["Max"] = await query.MaxAsync(maxLambda, cancellationToken);
                        break;

                    case AggregateFunction.Average:
                        var avgLambda = Expression.Lambda<Func<TResult, decimal?>>(Expression.Convert(property, typeof(decimal?)), parameter);
                        aggResults["Average"] = await query.AverageAsync(avgLambda, cancellationToken);
                        break;
                }
            }

            results[member] = aggResults;
        }

        return results;
    }
}

