using System.Linq.Expressions;
using LinqOp.Models;

namespace LinqOp.Extensions;

public static class EnumerableExtensions
{
    public static DataSourceResult<TResult> ToDataSourceResult<TResult>(this IEnumerable<TResult> source, DataSourceRequest request) where TResult : class
    {
        var query = source;

        // Apply Filters
        foreach (var filter in request.Filters)
        {
            var propertyInfo = typeof(TResult).GetProperty(filter.Member);
            if (propertyInfo == null) continue;

            var param = Expression.Parameter(typeof(TResult), "x");
            var member = Expression.PropertyOrField(param, filter.Member);
            var body = BuildFilterExpression(member, filter.Operator, filter.Value);

            if (body == null) continue;

            var lambda = Expression.Lambda<Func<TResult, bool>>(body, param).Compile();
            query = query.Where(lambda);
        }

        // Total before paging
        int total = query.Count();

        // Apply Sorting
        IOrderedEnumerable<TResult>? ordered = null;
        for (int i = 0; i < request.Sorts.Count; i++)
        {
            var sort = request.Sorts[i];
            var propertyInfo = typeof(TResult).GetProperty(sort.Member);
            if (propertyInfo == null) continue;

            var param = Expression.Parameter(typeof(TResult), "x");
            var property = Expression.PropertyOrField(param, sort.Member);
            var lambda = Expression.Lambda<Func<TResult, object>>(Expression.Convert(property, typeof(object)), param).Compile();

            if (i == 0)
                ordered = sort.Dir == SortDirection.Desc ? query.OrderByDescending(lambda) : query.OrderBy(lambda);
            else
                ordered = sort.Dir == SortDirection.Desc ? ordered!.ThenByDescending(lambda) : ordered!.ThenBy(lambda);
        }

        if (ordered != null)
            query = ordered;

        // Paging
        var items = query.Skip(request.Skip).Take(request.Take).ToList();

        return new DataSourceResult<TResult>(items, total);
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
}