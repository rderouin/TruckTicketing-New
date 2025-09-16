using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentValidation;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Product;
using SE.Shared.Domain.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.Sampling;
using SE.TruckTicketing.Domain.Entities.Sampling.Strategies;

using Trident.Business;
using Trident.Contracts;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Rules;

public class TruckTicketLandfillSamplingValidationRule : FluentValidationRule<TruckTicketEntity, TTErrorCodes>
{
    private readonly IManager<Guid, FacilityEntity> _facilityManager;

    private readonly IManager<Guid, FacilityServiceSubstanceIndexEntity> _facilityServiceSubstanceManager;

    // TODO: consolidate all TruckTicket workflow tasks/calls w/ a dataloader task
    private readonly IManager<Guid, LandfillSamplingEntity> _landfillSamplingManager;

    private readonly IManager<Guid, ProductEntity> _productManager;

    public TruckTicketLandfillSamplingValidationRule(IManager<Guid, LandfillSamplingEntity> landfillSamplingManager,
                                                     IManager<Guid, ProductEntity> productManager,
                                                     IManager<Guid, FacilityServiceSubstanceIndexEntity> facilityServiceSubstanceManager,
                                                     IManager<Guid, FacilityEntity> facilityManager)
    {
        _landfillSamplingManager = landfillSamplingManager;
        _productManager = productManager;
        _facilityServiceSubstanceManager = facilityServiceSubstanceManager;
        _facilityManager = facilityManager;
    }

    public override int RunOrder { get; } = 110;

    public override async Task Run(BusinessContext<TruckTicketEntity> context, List<ValidationResult> errors)
    {
        // this check is required when creating ticket stubs
        if (context.Target.FacilityServiceId != null || context.Target.FacilityServiceSubstanceId != Guid.Empty)
        {
            var facility = await _facilityManager.GetById(context.Target.FacilityId);
            context.ContextBag.TryAdd(nameof(facility), facility);

            var samplings = await _landfillSamplingManager.Get(s => s.FacilityId == context.Target.FacilityId);
            context.ContextBag.TryAdd(nameof(samplings), samplings);

            var facilityService = await _facilityServiceSubstanceManager.GetById(context.Target.FacilityServiceSubstanceId);
            var product = await _productManager.GetById(facilityService?.TotalProductId);
            var productNumber = product?.Number;
            context.ContextBag.TryAdd(nameof(productNumber), productNumber);
        }

        await base.Run(context, errors);
    }

    protected override void ConfigureRules(BusinessContext<TruckTicketEntity> context, InlineValidator<TruckTicketEntity> validator)
    {
        var facility = context.GetContextBagItemOrDefault<FacilityEntity>("facility");
        var samplings = context.GetContextBagItemOrDefault("samplings", new List<LandfillSamplingEntity>());
        var productNumber = context.GetContextBagItemOrDefault("productNumber", "");

        foreach (var sample in samplings)
        {
            var trackingStrategy = SampleTrackingStrategy.GetTrackingStrategy(sample.SamplingRuleType);
            var valueOverThreshold = trackingStrategy.OverCompareValue(sample, sample.Threshold, context.Target, context.Original);
            validator.RuleFor(truckTicket => truckTicket.LandfillSampled)
                     .Must(_ => !valueOverThreshold)
                     .When(truckTicket =>
                               truckTicket.Status == TruckTicketStatus.New &&
                               !truckTicket.LandfillSampled &&
                               facility?.Type == FacilityType.Lf &&
                               trackingStrategy.SampleAppliesToTicket(sample, context, productNumber))
                     .WithTridentErrorCode(TTErrorCodes.TruckTicket_LandfillSampleThreshold);
        }
    }
}
