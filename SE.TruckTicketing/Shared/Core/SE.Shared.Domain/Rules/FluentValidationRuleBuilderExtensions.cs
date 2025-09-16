using FluentValidation;

namespace SE.Shared.Domain.Rules;

public static class FluentValidationRuleBuilderExtensions
{
    public static IRuleBuilderOptions<TEntity, TProperty> WithState<TEntity, TProperty, TErrorCode>(this IRuleBuilderOptions<TEntity, TProperty> rule, ValidationResultState<TErrorCode> state)
        where TErrorCode : struct
    {
        return rule.WithState(_ => state);
    }

    public static IRuleBuilderOptions<TEntity, TProperty> WithTridentErrorCode<TEntity, TProperty, TErrorCode>(this IRuleBuilderOptions<TEntity, TProperty> rule, TErrorCode errorCode)
        where TErrorCode : struct
    {
        return rule.WithState(_ => new ValidationResultState<TErrorCode>(errorCode));
    }
}
