using FluentValidation;

using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;

namespace SE.Shared.Domain.Entities.AdditionalServicesConfiguration.Rules;

public class AdditionalServicesConfigurationValidationRules : FluentValidationRule<AdditionalServicesConfigurationEntity, TTErrorCodes>
{
    public override int RunOrder => 1100;

    protected override void ConfigureRules(BusinessContext<AdditionalServicesConfigurationEntity> context, InlineValidator<AdditionalServicesConfigurationEntity> validator)
    {
        validator.RuleFor(additionalServicesConfiguration => additionalServicesConfiguration.Name)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.AdditionalServicesConfiguration_Name);

        validator.RuleFor(additionalServicesConfiguration => additionalServicesConfiguration.FacilityId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.AdditionalServicesConfiguration_Facility);
    }
}
