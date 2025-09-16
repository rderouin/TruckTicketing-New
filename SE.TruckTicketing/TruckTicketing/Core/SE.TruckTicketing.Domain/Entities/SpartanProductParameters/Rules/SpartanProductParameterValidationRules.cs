using FluentValidation;

using SE.Shared.Domain;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;

namespace SE.TruckTicketing.Domain.Entities.SpartanProductParameters.Rules;

public class SpartanProductParameterValidationRules : FluentValidationRule<SpartanProductParameterEntity, TTErrorCodes>
{
    public override int RunOrder => 20;

    protected override void ConfigureRules(BusinessContext<SpartanProductParameterEntity> context, InlineValidator<SpartanProductParameterEntity> validator)
    {
        validator.RuleFor(param => param.ProductName)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.SpartanProductParam_ProductNameIsRequired);

        validator.RuleFor(param => param.ProductName)
                 .MaximumLength(100)
                 .WithTridentErrorCode(TTErrorCodes.SpartanProductParam_ProductNameLessThan100)
                 .When(x => !string.IsNullOrEmpty(x.ProductName));

        validator.RuleFor(param => param.MaxFluidDensity)
                 .GreaterThanOrEqualTo(x => x.MinFluidDensity)
                 .WithMessage("'Max Fluid Density' must be greater than or equal to 'Min Fluid Density'")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.SpartanProductParam_MaxLessThanMin, nameof(SpartanProductParameterEntity.MaxFluidDensity)));

        validator.RuleFor(param => param.MaxWaterPercentage)
                 .GreaterThanOrEqualTo(x => x.MinWaterPercentage)
                 .WithMessage("'Max Water Percentage' must be greater than or equal to 'Min Water Percentage'")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.SpartanProductParam_MaxLessThanMin, nameof(SpartanProductParameterEntity.MaxWaterPercentage)));

        validator.RuleFor(param => param.MinWaterPercentage)
                 .LessThanOrEqualTo(100)
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.SpartanProductParam_percentageLessThan100, nameof(SpartanProductParameterEntity.MinWaterPercentage)));

        validator.RuleFor(param => param.MaxWaterPercentage)
                 .GreaterThan(0)
                 .LessThanOrEqualTo(100)
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.SpartanProductParam_percentageLessThan100, nameof(SpartanProductParameterEntity.MaxWaterPercentage)));
    }
}
