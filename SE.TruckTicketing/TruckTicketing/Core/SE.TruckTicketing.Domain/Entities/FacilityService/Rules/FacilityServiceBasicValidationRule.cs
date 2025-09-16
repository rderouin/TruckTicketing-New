using FluentValidation;

using SE.Shared.Domain;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;

namespace SE.TruckTicketing.Domain.Entities.FacilityService.Rules;

public class
    FacilityServiceBasicValidationRule : FluentValidationRule<FacilityServiceEntity, TTErrorCodes>
{
    public override int RunOrder => 300;

    protected override void ConfigureRules(BusinessContext<FacilityServiceEntity> context, InlineValidator<FacilityServiceEntity> validator)
    {
        validator.RuleFor(fs => fs.ServiceTypeId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.FacilityService_ServiceTypeIdRequired);

        validator.RuleFor(fs => fs.ServiceNumber)
                 .GreaterThan(0)
                 .WithTridentErrorCode(TTErrorCodes.FacilityService_ServiceNumberPositive);

        validator.RuleFor(fs => fs.ServiceNumber)
                 .Must((entity, _) => entity.IsUnique.GetValueOrDefault(true))
                 .WithMessage(e => $"'Service Number' must be unique for the facility. A facility service already exists with number '{e.ServiceNumber}'.")
                 .WithTridentErrorCode(TTErrorCodes.FacilityService_ServiceNumberUnique);
    }
}
