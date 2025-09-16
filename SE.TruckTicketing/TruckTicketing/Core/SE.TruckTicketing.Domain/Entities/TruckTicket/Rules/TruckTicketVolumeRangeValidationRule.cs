using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentValidation;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Rules;

public class TruckTicketVolumeRangeValidationRule : FluentValidationRule<TruckTicketEntity, TTErrorCodes>
{
    private readonly IManager<Guid, ServiceTypeEntity> _serviceTypeManager;

    public TruckTicketVolumeRangeValidationRule(IManager<Guid, ServiceTypeEntity> serviceTypeManager)
    {
        _serviceTypeManager = serviceTypeManager;
    }

    public override int RunOrder => 120;

    protected override void ConfigureRules(BusinessContext<TruckTicketEntity> context, InlineValidator<TruckTicketEntity> validator)
    {
        var serviceType = context.GetContextBagItemOrDefault<ServiceTypeEntity>("serviceType");
        
        if (!(context.Original?.Status is not TruckTicketStatus.Approved &&
              context.Target.Status is TruckTicketStatus.Approved) || // Only run validation when attempting to transition to the approved status
            context.Operation is not Operation
               .Update || // Only run during an update. If this is an insert for reversals, service type validations may have changed and we dont want to jeopardize that operation
            context.Target.TruckTicketType is not (TruckTicketType.SP or TruckTicketType.WT) || serviceType == null)
        {
            return;
        }

        //Oil
        if (serviceType.IncludesOil)
        {
            if (serviceType.OilThresholdType == SubstanceThresholdType.Fixed)
            {
                validator.RuleFor(ticket => ticket.OilVolume)
                         .Must(oilFixedValue => oilFixedValue >= serviceType.OilMinValue && oilFixedValue <= serviceType.OilMaxValue)
                         .When(_ => serviceType.OilMinValue.HasValue && serviceType.OilMaxValue.HasValue)
                         .WithMessage($"Oil quantity is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeOilFixedBothOutOfRange);

                validator.RuleFor(ticket => ticket.OilVolume)
                         .Must(oilFixedValue => oilFixedValue >= serviceType.OilMinValue)
                         .When(_ => serviceType.OilMinValue.HasValue && !serviceType.OilMaxValue.HasValue)
                         .WithMessage($"Oil quantity is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeOilFixedLessThanMin);

                validator.RuleFor(ticket => ticket.OilVolume)
                         .Must(oilFixedValue => oilFixedValue <= serviceType.OilMaxValue)
                         .When(_ => !serviceType.OilMinValue.HasValue && serviceType.OilMaxValue.HasValue)
                         .WithMessage($"Oil quantity is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeOilFixedGreaterThanMax);
            }
            else if (serviceType.OilThresholdType == SubstanceThresholdType.Percentage)
            {
                validator.RuleFor(ticket => ticket.OilVolumePercent)
                         .Must(oilPercentageValue => oilPercentageValue >= serviceType.OilMinValue && oilPercentageValue <= serviceType.OilMaxValue)
                         .When(_ => serviceType.OilMinValue.HasValue && serviceType.OilMaxValue.HasValue)
                         .WithMessage($"Oil percentage is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeOilBothOutOfRange);

                validator.RuleFor(ticket => ticket.OilVolumePercent)
                         .Must(oilPercentageValue => oilPercentageValue >= serviceType.OilMinValue)
                         .When(_ => serviceType.OilMinValue.HasValue && !serviceType.OilMaxValue.HasValue)
                         .WithMessage($"Oil percentage is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeOilLessThanMin);

                validator.RuleFor(ticket => ticket.OilVolumePercent)
                         .Must(oilPercentageValue => oilPercentageValue <= serviceType.OilMaxValue)
                         .When(_ => !serviceType.OilMinValue.HasValue && serviceType.OilMaxValue.HasValue)
                         .WithMessage($"Oil percentage is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeOilGreaterThanMax);
            }
        }

        //Water
        if (serviceType.IncludesWater)
        {
            if (serviceType.WaterThresholdType == SubstanceThresholdType.Percentage)
            {
                validator.RuleFor(ticket => ticket.WaterVolumePercent)
                         .Must(waterFixedValue => waterFixedValue >= serviceType.WaterMinValue && waterFixedValue <= serviceType.WaterMaxValue)
                         .When(_ => serviceType.WaterMinValue.HasValue && serviceType.WaterMaxValue.HasValue)
                         .WithMessage($"Water percentage is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeWaterBothOutOfRange);

                validator.RuleFor(ticket => ticket.WaterVolumePercent)
                         .Must(waterFixedValue => waterFixedValue >= serviceType.WaterMinValue)
                         .When(_ => serviceType.WaterMinValue.HasValue && !serviceType.WaterMaxValue.HasValue)
                         .WithMessage($"Water percentage is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeWaterLessThanMin);

                validator.RuleFor(ticket => ticket.WaterVolumePercent)
                         .Must(waterFixedValue => waterFixedValue <= serviceType.WaterMaxValue)
                         .When(_ => !serviceType.WaterMinValue.HasValue && serviceType.WaterMaxValue.HasValue)
                         .WithMessage($"Water percentage is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeWaterGreaterThanMax);
            }
            else if (serviceType.WaterThresholdType == SubstanceThresholdType.Fixed)
            {
                validator.RuleFor(ticket => ticket.WaterVolume)
                         .Must(waterPercentageValue => waterPercentageValue >= serviceType.WaterMinValue && waterPercentageValue <= serviceType.WaterMaxValue)
                         .When(_ => serviceType.WaterMinValue.HasValue && serviceType.WaterMaxValue.HasValue)
                         .WithMessage($"Water quantity is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeWaterFixedBothOutOfRange);

                validator.RuleFor(ticket => ticket.WaterVolume)
                         .Must(waterPercentageValue => waterPercentageValue >= serviceType.WaterMinValue)
                         .When(_ => serviceType.WaterMinValue.HasValue && !serviceType.WaterMaxValue.HasValue)
                         .WithMessage($"Water quantity is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeWaterFixedLessThanMin);

                validator.RuleFor(ticket => ticket.WaterVolume)
                         .Must(waterPercentageValue => waterPercentageValue <= serviceType.WaterMaxValue)
                         .When(_ => !serviceType.WaterMinValue.HasValue && serviceType.WaterMaxValue.HasValue)
                         .WithMessage($"Water quantity is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeWaterFixedGreaterThanMax);
            }
        }

        //Solids
        if (serviceType.IncludesSolids)
        {
            if (serviceType.SolidThresholdType == SubstanceThresholdType.Percentage)
            {
                validator.RuleFor(ticket => ticket.SolidVolumePercent)
                         .Must(waterFixedValue => waterFixedValue >= serviceType.SolidMinValue && waterFixedValue <= serviceType.SolidMaxValue)
                         .When(_ => serviceType.SolidMinValue.HasValue && serviceType.SolidMaxValue.HasValue)
                         .WithMessage($"Solids percentage is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeSolidBothOutOfRange);

                validator.RuleFor(ticket => ticket.SolidVolumePercent)
                         .Must(waterFixedValue => waterFixedValue >= serviceType.SolidMinValue)
                         .When(_ => serviceType.SolidMinValue.HasValue && !serviceType.SolidMaxValue.HasValue)
                         .WithMessage($"Solids percentage is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeSolidLessThanMin);

                validator.RuleFor(ticket => ticket.SolidVolumePercent)
                         .Must(oilFixedValue => oilFixedValue <= serviceType.SolidMaxValue)
                         .When(_ => !serviceType.SolidMinValue.HasValue && serviceType.SolidMaxValue.HasValue)
                         .WithMessage($"Solids percentage is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeSolidGreaterThanMax);
            }
            else if (serviceType.SolidThresholdType == SubstanceThresholdType.Fixed)
            {
                validator.RuleFor(ticket => ticket.SolidVolume)
                         .Must(waterPercentageValue => waterPercentageValue >= serviceType.SolidMinValue && waterPercentageValue <= serviceType.SolidMaxValue)
                         .When(_ => serviceType.SolidMinValue.HasValue && serviceType.SolidMaxValue.HasValue)
                         .WithMessage($"Solids quantity is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeSolidFixedBothOutOfRange);

                validator.RuleFor(ticket => ticket.SolidVolume)
                         .Must(waterPercentageValue => waterPercentageValue >= serviceType.SolidMinValue)
                         .When(_ => serviceType.SolidMinValue.HasValue && !serviceType.SolidMaxValue.HasValue)
                         .WithMessage($"Solids quantity is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeSolidFixedLessThanMin);

                validator.RuleFor(ticket => ticket.SolidVolume)
                         .Must(waterPercentageValue => waterPercentageValue <= serviceType.SolidMaxValue)
                         .When(_ => !serviceType.SolidMinValue.HasValue && serviceType.SolidMaxValue.HasValue)
                         .WithMessage($"Solids quantity is outside the range allowed for the selected Service Type {serviceType.ServiceTypeId}")
                         .WithTridentErrorCode(TTErrorCodes.TruckTicket_VolumeSolidFixedGreaterThanMax);
            }
        }
    }

    public override async Task Run(BusinessContext<TruckTicketEntity> context, List<ValidationResult> errors)
    {
        if (context.Target.TruckTicketType is TruckTicketType.WT or TruckTicketType.SP && context.Target.ServiceTypeId != default)
        {
            var serviceType = await _serviceTypeManager.GetById(context.Target.ServiceTypeId);
            context.ContextBag.TryAdd(nameof(serviceType), serviceType);
        }

        await base.Run(context, errors);
    }
}
