using FluentValidation;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;

namespace SE.TruckTicketing.Domain.Entities.ServiceType.Rules;

public class ServiceTypeBasicValidationRules : FluentValidationRule<ServiceTypeEntity, TTErrorCodes>
{
    public override int RunOrder => 700;

    protected override void ConfigureRules(BusinessContext<ServiceTypeEntity> context, InlineValidator<ServiceTypeEntity> validator)
    {
        validator.RuleFor(serviceType => serviceType.ServiceTypeId)
                 .NotEmpty()
                 .MaximumLength(50)
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_ServiceTypeId);

        validator.RuleFor(serviceType => serviceType.CountryCode)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_CountryCode);

        validator.RuleFor(serviceType => serviceType.Name)
                 .NotEmpty()
                 .MaximumLength(50)
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_ServiceTypeName);

        validator.RuleFor(serviceType => serviceType.Class)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_Class);

        validator.RuleFor(serviceType => serviceType.TotalItemName)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_TotalItemProduct);

        validator.RuleFor(serviceType => serviceType.ReportAsCutType)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_ReportAsCutType);

        validator.RuleFor(serviceType => serviceType.Stream)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_Stream);

        validator.RuleFor(serviceType => serviceType.TotalMaxValue)
                 .GreaterThan(serviceType => serviceType.TotalMinValue)
                 .When(serviceType => serviceType.TotalItemId != default && serviceType.TotalMaxValue.HasValue && serviceType.TotalMinValue.HasValue)
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_MinPercentageGreaterThanMax);

        validator.RuleFor(serviceType => serviceType.TotalMaxValue)
                 .LessThanOrEqualTo(100)
                 .When(serviceType => serviceType.TotalItemId != default && serviceType.TotalMaxValue.HasValue)
                 .When(serviceType => serviceType.TotalThresholdType == SubstanceThresholdType.Percentage)
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_MaxPercentageGreaterThan100);

        validator.RuleFor(serviceType => serviceType.WaterMaxValue)
                 .GreaterThan(serviceType => serviceType.WaterMinValue)
                 .When(serviceType => serviceType.WaterItemId != default && serviceType.WaterMaxValue.HasValue && serviceType.WaterMinValue.HasValue)
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_MinPercentageGreaterThanMax);

        validator.RuleFor(serviceType => serviceType.WaterMaxValue)
                 .LessThanOrEqualTo(100)
                 .When(serviceType => serviceType.WaterItemId != default && serviceType.WaterMaxValue.HasValue)
                 .When(serviceType => serviceType.WaterThresholdType == SubstanceThresholdType.Percentage)
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_MaxPercentageGreaterThan100);

        validator.RuleFor(serviceType => serviceType.SolidMaxValue)
                 .GreaterThan(serviceType => serviceType.SolidMinValue)
                 .When(serviceType => serviceType.SolidItemId != default && serviceType.SolidMaxValue.HasValue && serviceType.SolidMinValue.HasValue)
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_MinPercentageGreaterThanMax);

        validator.RuleFor(serviceType => serviceType.SolidMaxValue)
                 .LessThanOrEqualTo(100)
                 .When(serviceType => serviceType.SolidItemId != default && serviceType.SolidMaxValue.HasValue)
                 .When(serviceType => serviceType.SolidThresholdType == SubstanceThresholdType.Percentage)
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_MaxPercentageGreaterThan100);

        validator.RuleFor(serviceType => serviceType.OilMaxValue)
                 .GreaterThan(serviceType => serviceType.OilMinValue)
                 .When(serviceType => serviceType.OilItemId != default && serviceType.OilMaxValue.HasValue && serviceType.OilMinValue.HasValue)
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_MinPercentageGreaterThanMax);

        validator.RuleFor(serviceType => serviceType.OilMaxValue)
                 .LessThanOrEqualTo(100)
                 .When(serviceType => serviceType.OilItemId != default && serviceType.OilMaxValue.HasValue)
                 .When(serviceType => serviceType.OilThresholdType == SubstanceThresholdType.Percentage)
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_MaxPercentageGreaterThan100);

        validator.RuleFor(serviceType => serviceType.ReportAsCutType)
                 .Equal(ReportAsCutTypes.AsPerCutsEntered)
                 .When(serviceType => serviceType.IncludesOil || serviceType.IncludesSolids || serviceType.IncludesWater)
                 .WithMessage("'Cut Type' must be 'As Per Cuts Entered' for services that include Oil, Water or Solid items.")
                 .WithTridentErrorCode(TTErrorCodes.ServiceType_InvalidReportAsCutTypeSelected);
    }
}
