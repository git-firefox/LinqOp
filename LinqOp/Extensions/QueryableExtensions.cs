using System.Linq.Expressions;
using System.Reflection;
using LinqOp.Models;
using Microsoft.EntityFrameworkCore;

namespace LinqOp.Extensions;

public static class QueryableExtensions
{
    public static async Task<DataSourceResult<T>> ToDataSourceResultAsync<T>(
        this IQueryable<T> query,
        DataSourceRequest request,
        CancellationToken cancellationToken = default) where T : class
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

        return new DataSourceResult<T>(data, total, aggregates);
    }

    private static IQueryable<TQuery> ApplyFilters<TQuery>(IQueryable<TQuery> query, IList<FilterDescriptor> filters)
    {
        foreach (var filter in filters)
        {
            var propertyInfo = typeof(TQuery).GetProperty(filter.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null) continue;

            var parameter = Expression.Parameter(typeof(TQuery), "x");
            var member = Expression.PropertyOrField(parameter, filter.Member);

            var body = BuildFilterExpression(member, filter.Operator, filter.Value);

            if (body == null) continue;

            var lambda = Expression.Lambda<Func<TQuery, bool>>(body, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    private static IQueryable<TQuery> ApplySorting<TQuery>(IQueryable<TQuery> query, IList<SortDescriptor> sorts)
    {
        bool first = true;

        foreach (var sort in sorts)
        {
            var propertyInfo = typeof(TQuery).GetProperty(sort.Member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null) continue;

            var parameter = Expression.Parameter(typeof(TQuery), "x");
            var member = Expression.PropertyOrField(parameter, sort.Member);
            var lambda = Expression.Lambda(member, parameter);

            string method = first
                ? (sort.Dir == SortDirection.Asc ? "OrderBy" : "OrderByDescending")
                : (sort.Dir == SortDirection.Asc ? "ThenBy" : "ThenByDescending");

            query = query.Provider.CreateQuery<TQuery>(
                Expression.Call(
                    typeof(Queryable),
                    method,
                    [typeof(TQuery), member.Type],
                    query.Expression,
                    Expression.Quote(lambda)));

            first = false;
        }

        return query;
    }

    private static async Task<IDictionary<string, IDictionary<string, object>>> ApplyAggregates<TQuery>(
            IQueryable<TQuery> query,
            IList<AggregateDescriptor> aggregates,
            CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, IDictionary<string, object>>();

        foreach (var group in aggregates.GroupBy(a => a.Member))
        {
            var member = group.Key;
            var prop = typeof(TQuery).GetProperty(member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) continue;

            var parameter = Expression.Parameter(typeof(TQuery), "x");
            var property = Expression.PropertyOrField(parameter, member);
            var lambda = Expression.Lambda(property, parameter);
            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var aggResults = new Dictionary<string, object>();

            foreach (var agg in group)
            {
                if (!IsAggregatable(targetType, agg.Aggregate))
                    continue;

                switch (agg.Aggregate)
                {
                    case AggregateFunction.Count:
                        aggResults["Count"] = await query.CountAsync(cancellationToken);
                        break;

                    case AggregateFunction.Sum:
                        var sumLambda = Expression.Lambda<Func<TQuery, decimal?>>(Expression.Convert(property, typeof(decimal?)), parameter);
                        aggResults["Sum"] = await query.SumAsync(sumLambda, cancellationToken);
                        break;

                    case AggregateFunction.Min:
                        var minLambda = Expression.Lambda<Func<TQuery, object>>(Expression.Convert(property, typeof(object)), parameter);
                        aggResults["Min"] = await query.MinAsync(minLambda, cancellationToken);
                        break;

                    case AggregateFunction.Max:
                        var maxLambda = Expression.Lambda<Func<TQuery, object>>(Expression.Convert(property, typeof(object)), parameter);
                        aggResults["Max"] = await query.MaxAsync(maxLambda, cancellationToken);
                        break;

                    case AggregateFunction.Average:
                        var avgLambda = Expression.Lambda<Func<TQuery, decimal?>>(Expression.Convert(property, typeof(decimal?)), parameter);
                        aggResults["Average"] = await query.AverageAsync(avgLambda, cancellationToken);
                        break;
                }
            }

            results[member] = aggResults;
        }

        return results;
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

