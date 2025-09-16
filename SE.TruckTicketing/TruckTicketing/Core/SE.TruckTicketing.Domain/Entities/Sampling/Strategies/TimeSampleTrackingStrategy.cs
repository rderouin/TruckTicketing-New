using System;

using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.Sampling.Strategies;

public class TimeSampleTrackingStrategy : SampleTrackingStrategyBase<DateTimeOffset>, ISampleTrackingStrategy
{
    public override string InitialThresholdValue() => DateTimeOffset.UtcNow.ToString();

    protected override LandfillSamplingEntity UpdateStrategy(LandfillSamplingEntity samplingEntity,
                                                             TruckTicketEntity targetTicket,
                                                             TruckTicketEntity originalTicket = null)
    {
        return samplingEntity;
    }

    protected override bool IsValueGreaterThanCompareValue(LandfillSamplingEntity sample, string compareValue)
    {
        if (int.TryParse(compareValue, out var numberOfDays) &&
            DateTimeOffset.TryParse(sample.Value, out var dateLastSampled))
        {   
            var dueDate = dateLastSampled.AddDays(numberOfDays);
            var nowUtc = DateTimeOffset.UtcNow;

            return nowUtc > dueDate;
        }

        return false;
    }

    protected override LandfillSamplingEntity AdjustSampling(LandfillSamplingEntity samplingEntity,
                                                     TruckTicketEntity ticket,
                                                     bool original)
    {
        return samplingEntity;
    }
}
