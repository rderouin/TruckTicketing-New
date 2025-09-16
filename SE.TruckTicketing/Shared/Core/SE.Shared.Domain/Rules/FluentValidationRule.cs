using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Results;

using Trident.Business;
using Trident.Domain;
using Trident.Validation;

using ValidationResult = Trident.Validation.ValidationResult;

namespace SE.Shared.Domain.Rules;

public abstract class FluentValidationRule<TEntity, TErrorCode> : ValidationRuleBase<BusinessContext<TEntity>>
    where TEntity : Entity
    where TErrorCode : struct
{
    public override Task Run(BusinessContext<TEntity> context, List<ValidationResult> errors)
    {
        var validator = new InlineValidator<TEntity>();
        ConfigureRules(context, validator);

        var result = validator.Validate(context.Target);
        errors.AddRange(result.Errors.Select(ToValidationResult));
        return Task.CompletedTask;
    }

    protected abstract void ConfigureRules(BusinessContext<TEntity> context, InlineValidator<TEntity> validator);

    private static ValidationResult<TErrorCode> ToValidationResult(ValidationFailure validationFailure)
    {
        var tridentValidationState = validationFailure.CustomState as ValidationResultState<TErrorCode>;
        var errorCode = tridentValidationState?.ErrorCode;
        var propertyNames = tridentValidationState?.PropertyNames?.Any() == true ? tridentValidationState.PropertyNames : new[] { validationFailure.PropertyName };

        return new(validationFailure.ErrorMessage, errorCode, propertyNames);
    }
}
