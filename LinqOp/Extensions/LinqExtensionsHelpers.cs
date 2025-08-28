using System.Linq.Expressions;
using LinqOp.Models;
using Microsoft.EntityFrameworkCore;

namespace LinqOp.Extensions
{
    public static class LinqExtensionsHelpers
    {
        public static Expression? BuildFilterExpression(MemberExpression member, FilterOperator op, string? value)
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
        public static bool IsAggregatable(Type type, AggregateFunction function)
        {
            if (function == AggregateFunction.Count)
                return true;

            if (IsNumericType(type) || type == typeof(DateTime))
                return true;

            return false;
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
    }
}