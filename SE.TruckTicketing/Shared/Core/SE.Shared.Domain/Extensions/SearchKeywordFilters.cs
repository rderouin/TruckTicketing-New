using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using Trident;
using Trident.Search;

namespace SE.Shared.Domain.Extensions;

public static class SearchKeywordFilters
{
    private static readonly Dictionary<CompareOperators, Func<Expression, Expression, BinaryExpression>>
        _operatorDict
            = new()
            {
                { CompareOperators.eq, Expression.Equal },
                { CompareOperators.ne, Expression.NotEqual },
                { CompareOperators.gt, Expression.GreaterThan },
                { CompareOperators.gte, Expression.GreaterThanOrEqual },
                { CompareOperators.lt, Expression.LessThan },
                { CompareOperators.lte, Expression.LessThanOrEqual },
                { CompareOperators.contains, (a, b) => TypeExtensions.GetStringEvalOperationExpression(nameof(string.Contains), a, b) },
                { CompareOperators.startsWith, (a, b) => TypeExtensions.GetStringEvalOperationExpression(nameof(string.StartsWith), a, b) },
                { CompareOperators.endWith, (a, b) => TypeExtensions.GetStringEvalOperationExpression(nameof(string.EndsWith), a, b) },
            };

    public static Expression<Func<T, bool>> CreateExpression<T>(this Axiom filter, bool toLower = false)
    {
        var ax = filter;
        Expression toLowerExpression = null;
        var param = Expression.Parameter(typeof(T), "x");
        Expression body = param;
        foreach (var member in ax.Field.Split('.'))
        {
            body = Expression.PropertyOrField(body, member);
        }

        var lambda = Expression.Lambda(body, param);
        if (toLower)
        {
            var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);
            toLowerExpression = Expression.Call(lambda.Body, toLowerMethod!);
        }

        var compareExpressionFunc = _operatorDict[ax.Operator];

        var filterValue = ResolveTypeBoxing(lambda.Body.Type, ax.Value);
        var constantExpression = lambda.Body.Type.IsNullableMember()
                                     ? Expression.Constant(filterValue, lambda.Body.Type)
                                     : Expression.Constant(filterValue);

        var equalExpression = compareExpressionFunc(toLowerExpression ?? lambda.Body, constantExpression);

        return Expression.Lambda<Func<T, bool>>(equalExpression, param);
    }

    private static object ResolveTypeBoxing(Type targetType, object value)
    {
        var filterValueType = value?.GetType();
        if (filterValueType == null)
        {
            return value;
        }

        if (filterValueType == typeof(string))
        {
            var typeDelegate = TypeExtensions.GetParserFunction(targetType);
            return typeDelegate(value as string);
        }

        if (filterValueType.IsPrimitive())
        {
            return value;
        }

        throw new NotSupportedException("Complex filter values are only supported using ComplexFilterStrategies.");
    }
}
