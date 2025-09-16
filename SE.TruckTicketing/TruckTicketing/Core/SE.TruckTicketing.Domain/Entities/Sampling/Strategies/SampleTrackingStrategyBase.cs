using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;

using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using TypeExtensions = Trident.TypeExtensions;

namespace SE.TruckTicketing.Domain.Entities.Sampling.Strategies;

public interface ISampleTrackingStrategy
{
    string InitialThresholdValue();

    bool OverCompareValue(
        LandfillSamplingEntity sample,
        string compareValue,
        TruckTicketEntity targetTicket,
        TruckTicketEntity originalTicket = null);

    LandfillSamplingEntity UpdateSample(LandfillSamplingEntity sample,
                                        TruckTicketEntity targetTicket,
                                        TruckTicketEntity originalTicket = null);

    bool SampleAppliesToTicket(LandfillSamplingEntity sampling,
                                             BusinessContext<TruckTicketEntity> context,
                                             string productNumber);

    bool SampleAppliesToTicket(LandfillSamplingEntity sampling,
                               TruckTicketEntity truckTicket,
                               string productNumber);
}

public abstract class SampleTrackingStrategyBase<TValueType> : ISampleTrackingStrategy where TValueType : IComparable
{
    public abstract string InitialThresholdValue();

    protected abstract LandfillSamplingEntity UpdateStrategy(LandfillSamplingEntity samplingEntity,
                                                             TruckTicketEntity targetTicket,
                                                             TruckTicketEntity originalTicket = null);

    protected abstract LandfillSamplingEntity AdjustSampling(LandfillSamplingEntity samplingEntity,
                                                     TruckTicketEntity ticket,
                                                     bool original);
    
    public bool OverCompareValue(LandfillSamplingEntity sample, string compareValue, TruckTicketEntity targetTicket, TruckTicketEntity originalTicket = null)
    {
        // create new sample so the original is never mutated
        var sampleToCompare = new LandfillSamplingEntity()
        {
            SamplingRuleType = sample.SamplingRuleType,
            Value = sample.Value,
            Threshold = sample.Threshold,
            WarningThreshold = sample.WarningThreshold,
            ProductNumberFilter = sample.ProductNumberFilter,
        };

        //if original == null, this is a creation context, we use update strat for all samples
        //if original !- null, are you a weight sampling
        if(originalTicket != null && originalTicket.LoadSamplingId == sampleToCompare.Id && sampleToCompare.SamplingRuleType == SamplingRuleType.Weight)
        {
            sampleToCompare = AdjustSampling(sampleToCompare, originalTicket, true);
            sampleToCompare = AdjustSampling(sampleToCompare, targetTicket, true);
        }
        else
        {
            sampleToCompare = UpdateStrategy(sampleToCompare, targetTicket, originalTicket);
        }
        
        return IsValueGreaterThanCompareValue(sampleToCompare, compareValue);
    }

    public LandfillSamplingEntity UpdateSample(LandfillSamplingEntity sample, TruckTicketEntity targetTicket, TruckTicketEntity originalTicket = null)
    {
        // reset sample value to initial value if the truck ticket was sampled 
        if (targetTicket.LandfillSampled)
        {
            sample.Value = InitialThresholdValue();
            sample.TimeOfLastLoadSample = DateTime.UtcNow;
            return sample;
        }
        
        // update the sample value based on the sample rule type
        return UpdateStrategy(sample, targetTicket, originalTicket);
    }

    public List<LandfillSamplingEntity> AdjustSamples(LandfillSamplingEntity targetSample, LandfillSamplingEntity originalSample, TruckTicketEntity targetTicket, TruckTicketEntity originalTicket = null)
    {
        List<LandfillSamplingEntity> samples = new List<LandfillSamplingEntity>();

        // if we have sampled the original before, the gross and net weight WERE non-zero.
        // we want to UNDO whatever this ticket did.
        if (originalTicket.LoadSamplingId.HasValue)
        {
            samples.Add(AdjustSampling(originalSample, originalTicket, true));
        }
        else // the original ticket never changed this, pass it back unmodified.
        {
            samples.Add(originalSample);
        }

        // reset sample value to initial value if the truck ticket was sampled
        if (targetTicket.LandfillSampled)
        {
            targetSample.Value = InitialThresholdValue();
            targetSample.TimeOfLastLoadSample = DateTime.UtcNow;
            samples.Add(targetSample);
        }
        else
        {
            samples.Add(AdjustSampling(targetSample, targetTicket, false));
        }

        // update the sample value based on the sample rule type
        return samples;
    }

    public bool SampleAppliesToTicket(LandfillSamplingEntity sampling, BusinessContext<TruckTicketEntity> context, string productNumber)
    {
        // skip load type sampling on update because they were already accounted for on create
        if (sampling.SamplingRuleType == SamplingRuleType.Load &&
            context.Operation == Operation.Update)
        {
            return false;
        }

        var result = SampleAppliesToTicket(sampling, context.Target, productNumber);
        return result;
    }
    
    public bool SampleAppliesToTicket(LandfillSamplingEntity sampling, TruckTicketEntity truckTicket, string productNumber)
    {
        // if product filter was specified and does match TruckTicket -> facilityservice -> product productNumber, ignore
        if (!string.IsNullOrEmpty(sampling.ProductNumberFilter) && sampling.ProductNumberFilter != GetProductNumberClassification(productNumber))
        {
            return false;
        }

        // if well classification was specified and does not match TruckTicket, ignore
        if (!sampling.WellClassificationFilters.IsEmpty() &&
            !sampling.WellClassificationFilters.Contains(truckTicket.WellClassification.ToString()))
        {
            return false;
        }

        return true;
    }
    
    private static string GetProductNumberClassification(string productNumber)
    {
        // class filters must be length of 3
        var productNumberClass = productNumber[..Math.Min(productNumber.Length, 3)];
        return productNumberClass;
    }
    
    protected virtual bool IsValueGreaterThanCompareValue(LandfillSamplingEntity sample, string compareValue) 
    {
        // will raise if value cannot be parsed
        var newValue = TypeExtensions.ParseToTypedObject(sample.Value, typeof(TValueType));
        var threshold = TypeExtensions.ParseToTypedObject(compareValue, typeof(TValueType));
        
        // generic greater than
        return ((TValueType)newValue).CompareTo((TValueType)threshold) > 0;
    }
}
