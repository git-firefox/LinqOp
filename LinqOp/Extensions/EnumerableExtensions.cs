using System.Linq.Expressions;
using System.Reflection;
using LinqOp.Models;

namespace LinqOp.Extensions;

public static class EnumerableExtensions
{
    public static DataSourceResult<TResult> ToDataSourceResult<TResult>(this IEnumerable<TResult> source, DataSourceRequest request) where TResult : class
    {
        var query = source;

        // Apply filtering
        if (request.Filters.Count != 0)
        {
            query = ApplyFilters(query, request.Filters);
        }

        // Total before paging
        int total = query.Count();

        // Apply Sorting
        if (request.Sorts.Count != 0)
        {
            query = ApplySorting(query, request.Sorts);
        }

        IDictionary<string, IDictionary<string, object>>? aggregates = null;
        if (request.Aggregates.Count != 0)
        {
            aggregates = ApplyAggregates(query, request.Aggregates);
        }

        // Apply Aggregates
        if (request.Take <= 0)
        {
            request.Take = 10; // set default to 10
        }
        var items = query.Skip(request.Skip).Take(request.Take).ToList();

        return new DataSourceResult<TResult>(items, total, aggregates);
    }

    private static IEnumerable<TResult> ApplyFilters<TResult>(IEnumerable<TResult> query, IList<FilterDescriptor> filters)
    {
        foreach (var filter in filters)
        {
            var propertyInfo = typeof(TResult).GetProperty(filter.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null) continue;

            var param = Expression.Parameter(typeof(TResult), "x");
            var member = Expression.PropertyOrField(param, filter.Member);
            var body = LinqExtensionsHelpers.BuildFilterExpression(member, filter.Operator, filter.Value);

            if (body == null) continue;

            var lambda = Expression.Lambda<Func<TResult, bool>>(body, param).Compile();
            query = query.Where(lambda);
        }

        return query;
    }

    private static IEnumerable<TResult> ApplySorting<TResult>(IEnumerable<TResult> query, IList<SortDescriptor> sorts)
    {
        bool first = true;
        IOrderedEnumerable<TResult>? ordered = null;
        foreach (var sort in sorts)
        {
            var propertyInfo = typeof(TResult).GetProperty(sort.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null) continue;

            var parameter = Expression.Parameter(typeof(TResult), "x");
            var member = Expression.PropertyOrField(parameter, sort.Member);
            var lambda = Expression.Lambda<Func<TResult, object>>(Expression.Convert(member, typeof(object)), parameter).Compile();

            if (first)
                ordered = sort.Dir == SortDirection.Desc ? query.OrderByDescending(lambda) : query.OrderBy(lambda);
            else
                ordered = sort.Dir == SortDirection.Desc ? ordered!.ThenByDescending(lambda) : ordered!.ThenBy(lambda);

            first = false;
        }
        return query;
    }

    private static IDictionary<string, IDictionary<string, object>> ApplyAggregates<TResult>(
            IEnumerable<TResult> query,
            IList<AggregateDescriptor> aggregates)
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

            List<object?> values = [.. query.Select(x => prop.GetValue(x, null)).Where(v => v != null)];
            var nonNullValues = values.Where(v => v != null).ToList();

            foreach (var agg in group)
            {
                if (!LinqExtensionsHelpers.IsAggregatable(targetType, agg.Aggregate))
                    continue;

                object? result = null;
                switch (agg.Aggregate)
                {
                    case AggregateFunction.Count:
                        //result = values.Count;
                        result = query.Count();
                        break;

                    case AggregateFunction.Sum:
                        //result = nonNullValues.OfType<IConvertible>().Any() ? nonNullValues.OfType<IConvertible>().Sum(v => Convert.ToDecimal(v)) : null;
                        var sumLambda = Expression.Lambda<Func<TResult, decimal?>>(Expression.Convert(property, typeof(decimal?)), parameter).Compile();
                        result = query.Sum(sumLambda);
                        break;

                    case AggregateFunction.Min:
                        var minLambda = Expression.Lambda<Func<TResult, object>>(Expression.Convert(property, typeof(object)), parameter).Compile();
                        result = query.Min(minLambda);
                        //result = nonNullValues.Any() ? nonNullValues.Min()! : null;
                        break;

                    case AggregateFunction.Max:
                        var maxLambda = Expression.Lambda<Func<TResult, object>>(Expression.Convert(property, typeof(object)), parameter).Compile();
                        result = query.Max(maxLambda);
                        //result = nonNullValues.Any() ? nonNullValues.Max()! : null
                        break;

                    case AggregateFunction.Average:
                        var avgLambda = Expression.Lambda<Func<TResult, decimal?>>(Expression.Convert(property, typeof(decimal?)), parameter).Compile();
                        result = query.Average(avgLambda);
                        //result = nonNullValues.OfType<IConvertible>().Any() ? nonNullValues.OfType<IConvertible>().Average(v => Convert.ToDecimal(v)) : null
                        break;
                }

                if (result != null)
                    aggResults[agg.Aggregate.ToString().ToLower()] = result;
            }

            results[member] = aggResults;
        }

        return results;
    }
}