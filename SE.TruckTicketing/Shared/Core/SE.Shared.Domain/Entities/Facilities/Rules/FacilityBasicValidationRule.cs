using FluentValidation;

using SE.Shared.Domain.Entities.Facilities.Tasks;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;

namespace SE.Shared.Domain.Entities.Facilities.Rules;

public class FacilityBasicValidationRule : FluentValidationRule<FacilityEntity, TTErrorCodes>
{
    public override int RunOrder => 100;

    protected override void ConfigureRules(BusinessContext<FacilityEntity> context, InlineValidator<FacilityEntity> validator)
    {
        validator.RuleFor(facility => facility.SiteId)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.Facility_SiteId);

        validator.RuleFor(facility => facility.Name)
                 .NotEmpty()
                 .WithTridentErrorCode(TTErrorCodes.Facility_Name);

        validator.RuleForEach(facility => facility.WeightConversionParameters)
                 .ChildRules(preset => preset
                                      .RuleFor(x => x.StartDate)
                                      .Must(x => x != default)
                                      .WithMessage("Start Date required for default density conversion to apply to 'By Weight' calculation")
                                      .WithTridentErrorCode(TTErrorCodes.Facility_DefaultDensityConversionFactor_StartDate_Required_ByWeight))
                 .When(x => x.WeightConversionParameters is { Count: > 0 });

        validator.RuleForEach(facility => facility.MidWeightConversionParameters)
                 .ChildRules(preset => preset
                                      .RuleFor(x => x.StartDate)
                                      .Must(x => x != default)
                                      .WithMessage("Start Date required for default density conversion to apply to 'By Mid-Weight' calculation")
                                      .WithTridentErrorCode(TTErrorCodes.Facility_DefaultDensityConversionFactor_StartDate_Required_ByMidWeight))
                 .When(x => x.MidWeightConversionParameters is { Count: > 0 });

        validator.RuleFor(facility => facility.WeightConversionParameters)
                 .Must(_ => !DefaultDensityByWeightOverlappingFacilityService(context))
                 .WithMessage("Pre-Set default density by weight with same Source Location & overlapping Facility Services combination already exist.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.Facility_DefaultDensityConversionFactor_OverlappingFacilityService_ByWeight));

        validator.RuleFor(facility => facility.WeightConversionParameters)
                 .Must(_ => !DefaultDensityByMidWeightOverlappingFacilityService(context))
                 .WithMessage("Pre-Set default density by mid-weight with same Source Location & overlapping Facility Services combination already exist.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.Facility_DefaultDensityConversionFactor_OverlappingFacilityService_ByMidWeight));

        validator.RuleFor(facility => facility.WeightConversionParameters)
                 .Must(_ => !DefaultDensityByWeightOverlappingDensitiesByTimePeriod(context))
                 .WithMessage("Pre-Set default density by weight with overlapping time period identified.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.Facility_DefaultDensityConversionFactor_OverlappingTimePeriod_ByWeight));

        validator.RuleFor(facility => facility.WeightConversionParameters)
                 .Must(_ => !DefaultDensityByMidWeightOverlappingDensitiesByTimePeriod(context))
                 .WithMessage("Pre-Set default density by mid-weight with overlapping time period identified.")
                 .WithState(new ValidationResultState<TTErrorCodes>(TTErrorCodes.Facility_DefaultDensityConversionFactor_OverlappingTimePeriod_ByMidWeight));
    }

    private static bool DefaultDensityByWeightOverlappingFacilityService(BusinessContext<FacilityEntity> context)
    {
        return context.GetContextBagItemOrDefault(FacilityWorkflowContextBagKeys.OverlappingFacilityServiceForDefaultDensitiesByWeight, false);
    }

    private static bool DefaultDensityByMidWeightOverlappingFacilityService(BusinessContext<FacilityEntity> context)
    {
        return context.GetContextBagItemOrDefault(FacilityWorkflowContextBagKeys.OverlappingFacilityServiceForDefaultDensitiesByMidWeight, false);
    }

    private static bool DefaultDensityByWeightOverlappingDensitiesByTimePeriod(BusinessContext<FacilityEntity> context)
    {
        return context.GetContextBagItemOrDefault(FacilityWorkflowContextBagKeys.OverlappingDefaultDensitiesByTimePeriodForWeight, false);
    }

    private static bool DefaultDensityByMidWeightOverlappingDensitiesByTimePeriod(BusinessContext<FacilityEntity> context)
    {
        return context.GetContextBagItemOrDefault(FacilityWorkflowContextBagKeys.OverlappingDefaultDensitiesByTimePeriodForMidWeight, false);
    }
}
