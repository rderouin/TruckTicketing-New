using SE.Shared.Domain.Entities.TruckTicket;

namespace SE.TruckTicketing.Domain.Entities.Sampling.Strategies;

public class LoadSampleTrackingStrategy : SampleTrackingStrategyBase<int>, ISampleTrackingStrategy
{
    public override string InitialThresholdValue() => "0";

    protected override LandfillSamplingEntity UpdateStrategy(LandfillSamplingEntity samplingEntity,
                                                             TruckTicketEntity targetTicket,
                                                             TruckTicketEntity originalTicket = null)
    {
        if (int.TryParse(samplingEntity.Value, out var numberOfLoads))
        {
            numberOfLoads += 1;
            samplingEntity.Value = numberOfLoads.ToString();
        }
        return samplingEntity;
    }

    protected override LandfillSamplingEntity AdjustSampling(LandfillSamplingEntity samplingEntity,
                                                     TruckTicketEntity ticket,
                                                     bool original)
    {
        return samplingEntity;
    }
}
