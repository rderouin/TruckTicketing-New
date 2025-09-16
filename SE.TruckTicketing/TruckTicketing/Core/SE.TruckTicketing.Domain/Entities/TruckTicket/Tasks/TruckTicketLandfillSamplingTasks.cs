using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.Sampling;
using SE.TruckTicketing.Domain.Entities.Sampling.Strategies;
using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketLandfillSampleTakenTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    public override int RunOrder => 50;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        if (context.Operation == Operation.Insert && context.Target.LandfillSampledTime == null)
        {
            UpdateLandfillSampleData(context.Target);
        }

        return await Task.FromResult(true);
    }

    public override async Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        // only run for truck ticket with status "New"
        if (context.Target.Status != TruckTicketStatus.New)
        {
            return await Task.FromResult(false);
        }

        // don't run for stub tickets (assume stub ticket if FacilityServiceSubstanceId is empty)
        if (context.Target.FacilityServiceSubstanceId == Guid.Empty)
        {
            return await Task.FromResult(false);
        }

        // only run for landfill tickets
        // facility is currently fetched during validation - TruckTicketLandfillSamplingValidationRules
        var facility = context.GetContextBagItemOrDefault<FacilityEntity>("facility");
        return await Task.FromResult(facility.Type == FacilityType.Lf);
    }

    private void UpdateLandfillSampleData(TruckTicketEntity truckTicket)
    {
        truckTicket.LandfillSampledTime = DateTimeOffset.Now;
    }
}

public class TruckTicketLandfillSamplingUpdateTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly IManager<Guid, LandfillSamplingEntity> _landfillSamplingManager;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    public TruckTicketLandfillSamplingUpdateTask(IManager<Guid, LandfillSamplingEntity> landfillSamplingManager,
                                                 IProvider<Guid, TruckTicketEntity> truckTicketProvider)
    {
        _landfillSamplingManager = landfillSamplingManager;
        _truckTicketProvider = truckTicketProvider;
    }

    public override int RunOrder => 20000;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var samplings = await _landfillSamplingManager.Get(s => s.FacilityId == context.Target.FacilityId);
        // productNumber is currently fetched during validation - TruckTicketLandfillSamplingValidationRules
        var productNumber = context.GetContextBagItemOrDefault("productNumber", "");
        // if this is a creation context we want to update all possible sampling types
        if (context.Original == null)
        {
            await UpdateSamplings(context, samplings, productNumber);
        }
        // if this is an update context, we support updating only samples with a samplingRuleType of 'weight'
        else
        {
            samplings = samplings.Where(x => x.SamplingRuleType == SamplingRuleType.Weight);
            await AdjustWeightSamplings(context, samplings, productNumber);
        }

        return true;
    }

    public override async Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        // only run when gross weight and tare weight are greater than 0 - IsReadyToTakeSample flag handles this check
        // and when gross weight or tare weight values are being updated from 0 to a number greater than 0
        // we dont want to change/update sampling when it was taken at time of ticket creation or when ticket was later updated to take sampling - checking gross and tare weight original values handles this check
        var shouldRun = context.Target.IsReadyToTakeSample == true && (context.Original == null ||
                                                                       context.Original?.GrossWeight != context.Target.GrossWeight ||
                                                                       context.Original?.TareWeight != context.Target.TareWeight ||
                                                                       context.Original?.MaterialApprovalId != context.Target.MaterialApprovalId ||
                                                                       context.Original?.LandfillSampled != context.Target.LandfillSampled ||
                                                                       (context.Original?.Status != TruckTicketStatus.Void && context.Target.Status == TruckTicketStatus.Void));
        if (!shouldRun)
        {
            return await Task.FromResult(false);
        }        

        // don't run for stub tickets (assume stub ticket if FacilityServiceSubstanceId is empty)
        if (context.Target.FacilityServiceSubstanceId == Guid.Empty)
        {
            return await Task.FromResult(false);
        }

        // only run for landfill tickets
        // facility is currently fetched during validation - TruckTicketLandfillSamplingValidationRules
        var facility = context.GetContextBagItemOrDefault<FacilityEntity>("facility");
        return await Task.FromResult(facility.Type == FacilityType.Lf);
    }

    private async Task UpdateSamplings(BusinessContext<TruckTicketEntity> context, IEnumerable<LandfillSamplingEntity> samplings, string productNumber)
    {
        foreach (var sampling in samplings)
        {
            var sampleTrackingStrategy = SampleTrackingStrategy.GetTrackingStrategy(sampling.SamplingRuleType);
            if (!sampleTrackingStrategy.SampleAppliesToTicket(sampling, context, productNumber))
            {
                continue;
            }

            sampleTrackingStrategy.UpdateSample(sampling, context.Target, context.Original);

            await _landfillSamplingManager.Update(sampling, true);

            // should only be set once unless the sampling changes entirely in AdjustWeightSamplings
            context.Target.LoadSamplingId = context.Target.LoadSamplingId.HasValue ? context.Target.LoadSamplingId : sampling.Id;
            context.Target.TimeOfLastSampleCountdownUpdate = context.Target.TimeOfLastSampleCountdownUpdate.HasValue ? context.Target.TimeOfLastSampleCountdownUpdate : DateTime.UtcNow;
            await _truckTicketProvider.Update(context.Target, true);
        }
    }

    private async Task AdjustWeightSamplings(BusinessContext<TruckTicketEntity> context, IEnumerable<LandfillSamplingEntity> samplings, string productNumber)
    {
        foreach (var sampling in samplings)
        {
            var trackingStrategy = new WeightSampleTrackingStrategy();
            if (!trackingStrategy.SampleAppliesToTicket(sampling, context, productNumber))
            {
                continue;
            }

            if (context.Original.LoadSamplingId != sampling.Id) 
            {
                // it is necessary for us to update the sampling reference and time for the ticket when the sampling it modified changes
                // must be done before check
                context.Target.LoadSamplingId = sampling.Id;
                context.Target.TimeOfLastSampleCountdownUpdate = DateTime.UtcNow;
            }   

            LandfillSamplingEntity originalSampling;

            // the system must recall the original sampling if one exists for the ticket
            if (context.Original.LoadSamplingId.HasValue && context.Original.Id != Guid.Empty)
            {
                if (sampling.Id == context.Original.LoadSamplingId)
                {
                    originalSampling = sampling;
                }
                else
                {
                    originalSampling = _landfillSamplingManager.GetById(context.Original.LoadSamplingId).Result;
                }
            }
            // if none exists the target can be assumed to be the original provided there are no class changes; SHOULD I STILL USE THIS?
            else if (context.Original.ServiceTypeClass == context.Target.ServiceTypeClass)
            {
                originalSampling = sampling;
            }
            // if we haven't determined the original sampling yet, fetch and filter for the applicable sampling for the original facility id
            else
            {
                var originalFacilitySamplings = await _landfillSamplingManager.Get(s => s.FacilityId == context.Original.FacilityId);

                originalSampling = originalFacilitySamplings.Where(s => s.SamplingRuleType == SamplingRuleType.Weight).First();
            }

            // if the ticket does not have a TSU, the system assumes the Load Date to be the TSU (for historical tickets)
            var timeOfLastSampleCountdownUpdate = context.Target.TimeOfLastSampleCountdownUpdate.HasValue ? context.Target.TimeOfLastSampleCountdownUpdate : context.Target.LoadDate;

            // we must have an original sampling, and both samplings must have valid TimeOfLastLoadSample, and the TSU of the ticket must be exclusively AFTER the TLLS for both samplings
            if (originalSampling == null ||
                sampling.TimeOfLastLoadSample == null || sampling.TimeOfLastLoadSample == default(DateTime) ||
                originalSampling.TimeOfLastLoadSample == null || originalSampling.TimeOfLastLoadSample == default(DateTime) ||
                timeOfLastSampleCountdownUpdate < originalSampling.TimeOfLastLoadSample ||
                timeOfLastSampleCountdownUpdate < sampling.TimeOfLastLoadSample)
            {
                continue;
            }

            // if the system determines samplings are both valid for updates, then the system must update samplings based on the calculated net weight
            // - if the original net weight is positive the system must deduct the original net weight from the original sampling
            // - if the target net weight is positive the system must add the target net weight to the target sampling
            var samples = trackingStrategy.AdjustSamples(sampling, originalSampling, context.Target, context.Original);

            var targetSample = samples[0];
            var originalSample = samples[1];

            if (context.Original.LoadSamplingId != sampling.Id)
            {
                // if the original and target sampling are different, we must save both
                await _landfillSamplingManager.Update(originalSample, true);
                await _landfillSamplingManager.Update(targetSample, true);

                await _truckTicketProvider.Update(context.Target, true);
            }
            else
            {
                // the original sampling and target sampling are the same; one save
                if (context.Target.LandfillSampled)
                {
                    sampling.TimeOfLastLoadSample = DateTime.UtcNow;
                }
                await _landfillSamplingManager.Update(sampling, true);
            }
        }
    }
}
