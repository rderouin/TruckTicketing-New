using System;
using System.Globalization;

using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.Sampling.Strategies;

public class WeightSampleTrackingStrategy : SampleTrackingStrategyBase<double>, ISampleTrackingStrategy
{
    public override string InitialThresholdValue()
    {
        return "0";
    }

    protected override LandfillSamplingEntity UpdateStrategy(LandfillSamplingEntity samplingEntity,
                                                             TruckTicketEntity targetTicket,
                                                             TruckTicketEntity originalTicket = null)
    {
        // return if sample value cannot be parsed
        if (!Double.TryParse(samplingEntity.Value, out var currentWeight))
        {
            return samplingEntity;
        }

        // discard negative values
        var newWeight = currentWeight + (targetTicket.NetWeight > 0 ? targetTicket.NetWeight : 0);
        samplingEntity.Value = newWeight.ToString(CultureInfo.InvariantCulture);

        return samplingEntity;
    }

    protected override LandfillSamplingEntity AdjustSampling(LandfillSamplingEntity samplingEntity,
                                                     TruckTicketEntity ticket,
                                                     bool original)
    {
        if (!Double.TryParse(samplingEntity.Value, out var currentWeight) || ticket.Status == Contracts.Lookups.TruckTicketStatus.Void)
        {
            return samplingEntity;
        }

        double newWeight;

        // discard negative values : if this netweight is negative, it was treated as zero before, treat it as zero again
        if (original)
        {
            newWeight = currentWeight - (ticket.NetWeight > 0 ? ticket.NetWeight : 0);
        }
        else
        {
            newWeight = currentWeight + (ticket.NetWeight > 0 ? ticket.NetWeight : 0);
        }
        
        samplingEntity.Value = newWeight.ToString(CultureInfo.InvariantCulture);

        return samplingEntity;
    }
}
