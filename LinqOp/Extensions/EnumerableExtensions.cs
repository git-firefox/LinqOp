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

        //IDictionary<string, IDictionary<string, object>>? aggregates = null;
        //if (request.Aggregates.Count != 0)
        //{
        //    aggregates = ApplyAggregates(query, request.Aggregates);
        //}

        // Paging
        if (request.Take <= 0)
        {
            request.Take = 10; // set default to 10
        }
        var items = query.Skip(request.Skip).Take(request.Take).ToList();

        // Aggregates
        IDictionary<string, IDictionary<string, object>>? aggregates = null;
        if (request.Aggregates.Any())
        {
            aggregates = new Dictionary<string, IDictionary<string, object>>();

            foreach (var group in request.Aggregates.GroupBy(a => a.Member))
            {
                var dict = new Dictionary<string, object>();

                var prop = typeof(TResult).GetProperty(group.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop == null) continue;

                List<object?> values = [.. query.Select(x => prop.GetValue(x, null)).Where(v => v != null)];

                Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                var nonNullValues = values.Where(v => v != null).ToList();

                foreach (var agg in group)
                {
                    if (!IsAggregatable(targetType, agg.Aggregate))
                        continue;

                    object? result = agg.Aggregate switch
                    {
                        AggregateFunction.Sum => nonNullValues.OfType<IConvertible>().Any()
                            ? nonNullValues.OfType<IConvertible>().Sum(v => Convert.ToDecimal(v))
                            : null,

                        AggregateFunction.Min => nonNullValues.Any() ? nonNullValues.Min()! : null,

                        AggregateFunction.Max => nonNullValues.Any() ? nonNullValues.Max()! : null,

                        AggregateFunction.Average => nonNullValues.OfType<IConvertible>().Any()
                            ? nonNullValues.OfType<IConvertible>().Average(v => Convert.ToDecimal(v))
                            : null,

                        AggregateFunction.Count => values.Count,

                        _ => null
                    };

                    if (result != null)
                        dict[agg.Aggregate.ToString().ToLower()] = result;
                }

                if (dict.Any())
                    aggregates[group.Key] = dict;
            }
        }

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
            var body = BuildFilterExpression(member, filter.Operator, filter.Value);

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

    private static async Task<IDictionary<string, IDictionary<string, object>>> ApplyAggregates<TResult>(
            IEnumerable<TResult> query,
            IList<AggregateDescriptor> aggregates)
    {
        //var results = new Dictionary<string, IDictionary<string, object>>();

        //foreach (var group in aggregates.GroupBy(a => a.Member))
        //{
        //    var member = group.Key;
        //    var prop = typeof(TResult).GetProperty(member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        //    if (prop == null) continue;

        //    var parameter = Expression.Parameter(typeof(TResult), "x");
        //    var property = Expression.PropertyOrField(parameter, member);
        //    var lambda = Expression.Lambda(property, parameter);
        //    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        //    var aggResults = new Dictionary<string, object>();

        //    foreach (var agg in group)
        //    {
        //        if (!IsAggregatable(targetType, agg.Aggregate))
        //            continue;

        //        switch (agg.Aggregate)
        //        {
        //            case AggregateFunction.Count:
        //                aggResults["Count"] = await query.CountAsync(cancellationToken);
        //                break;

        //            case AggregateFunction.Sum:
        //                var sumLambda = Expression.Lambda<Func<TResult, decimal?>>(Expression.Convert(property, typeof(decimal?)), parameter);
        //                aggResults["Sum"] = await query.SumAsync(sumLambda, cancellationToken);
        //                break;

        //            case AggregateFunction.Min:
        //                var minLambda = Expression.Lambda<Func<TResult, object>>(Expression.Convert(property, typeof(object)), parameter);
        //                aggResults["Min"] = await query.MinAsync(minLambda, cancellationToken);
        //                break;

        //            case AggregateFunction.Max:
        //                var maxLambda = Expression.Lambda<Func<TResult, object>>(Expression.Convert(property, typeof(object)), parameter);
        //                aggResults["Max"] = await query.MaxAsync(maxLambda, cancellationToken);
        //                break;

        //            case AggregateFunction.Average:
        //                var avgLambda = Expression.Lambda<Func<TResult, decimal?>>(Expression.Convert(property, typeof(decimal?)), parameter);
        //                aggResults["Average"] = await query.AverageAsync(avgLambda, cancellationToken);
        //                break;
        //        }
        //    }

        //    results[member] = aggResults;
        //}

        //return results;
        return default!;
    }

    private static (ConstantExpression? ConstantExpression, Type? Type) GetConstant(MemberExpression member, string? value)
    {
        if (value == default)
            return (Expression.Constant(null, member.Type), null);
        try
        {
            var targetType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
            var typedValue = Convert.ChangeType(value, targetType);
            return (Expression.Constant(typedValue, member.Type), targetType);
        }
        catch (Exception)
        {
            return default;
        }
    }

    private static Expression? BuildFilterExpression(MemberExpression member, FilterOperator op, string? value)
    {
        var constant = GetConstant(member, value);
        if (constant != default)
        {
            Type? type = constant.Type;
            ConstantExpression constantExpression = constant.ConstantExpression!;
            if (type == typeof(string))
                return BuildStringExpression(member, op, constantExpression);

            if (type == typeof(bool))
                return BuildBooleanExpression(member, op, constantExpression);

            if (type != null && (type.IsPrimitive || type == typeof(decimal) || type == typeof(DateTime)))
                return BuildComparableExpression(member, op, constantExpression);
        }
        return null;
    }

    private static Expression BuildStringExpression(Expression property, FilterOperator op, Expression constant)
    {
        return op switch
        {
            //"eq" => Expression.Equal(property, constant),
            FilterOperator.Neq => Expression.NotEqual(property, constant),
            FilterOperator.Contains => Expression.Call(property, nameof(string.Contains), null, constant),
            FilterOperator.DoesNotContain => Expression.Not(Expression.Call(property, nameof(string.Contains), null, constant)),
            FilterOperator.StartsWith => Expression.Call(property, nameof(string.StartsWith), null, constant),
            FilterOperator.EndsWith => Expression.Call(property, nameof(string.EndsWith), null, constant),

            FilterOperator.IsNull => Expression.Equal(property, Expression.Constant(null, property.Type)),
            FilterOperator.IsNotNull => Expression.NotEqual(property, Expression.Constant(null, property.Type)),
            FilterOperator.IsEmpty => Expression.Equal(property, Expression.Constant(string.Empty)),
            FilterOperator.IsNotEmpty => Expression.NotEqual(property, Expression.Constant(string.Empty)),

            _ => Expression.Equal(property, constant),
        };
    }

    private static Expression BuildComparableExpression(Expression property, FilterOperator op, Expression constant)
    {
        return op switch
        {
            //"eq" => Expression.Equal(property, constant),
            FilterOperator.Neq => Expression.NotEqual(property, constant),
            FilterOperator.Gte => Expression.GreaterThanOrEqual(property, constant),
            FilterOperator.Gt => Expression.GreaterThan(property, constant),
            FilterOperator.Lte => Expression.LessThanOrEqual(property, constant),
            FilterOperator.Lt => Expression.LessThan(property, constant),
            FilterOperator.IsNull => Expression.Equal(property, Expression.Constant(null, property.Type)),
            FilterOperator.IsNotNull => Expression.NotEqual(property, Expression.Constant(null, property.Type)),
            _ => Expression.Equal(property, constant),
        };
    }

    private static Expression BuildBooleanExpression(Expression property, FilterOperator op, Expression constant)
    {
        return op switch
        {
            //"eq" => Expression.Equal(property, constant),
            FilterOperator.Neq => Expression.NotEqual(property, constant),
            FilterOperator.IsNull => Expression.Equal(property, Expression.Constant(null, property.Type)),
            FilterOperator.IsNotNull => Expression.NotEqual(property, Expression.Constant(null, property.Type)),
            _ => Expression.Equal(property, constant),
        };
    }

    private static bool IsNumericType(Type type)
    {
        if (type.IsEnum) return false;

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    private static bool IsAggregatable(Type type, AggregateFunction function)
    {
        if (function == AggregateFunction.Count)
            return true;

        if (IsNumericType(type) || type == typeof(DateTime))
            return true;

        return false;
    }

}