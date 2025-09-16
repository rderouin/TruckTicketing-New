using System;

using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Domain.Entities.Sampling.Strategies;

public static class SampleTrackingStrategy
{
    public static ISampleTrackingStrategy GetTrackingStrategy(SamplingRuleType ruleType)
    {
        return ruleType switch
               {
                   SamplingRuleType.Load => new LoadSampleTrackingStrategy(),
                   SamplingRuleType.Time => new TimeSampleTrackingStrategy(),
                   SamplingRuleType.Weight => new WeightSampleTrackingStrategy(),
                   
                   SamplingRuleType.Unspecified => throw new ArgumentOutOfRangeException(nameof(ruleType), ruleType, null),
                   _ => throw new ArgumentOutOfRangeException(nameof(ruleType), ruleType, null),
               };
    }
}
