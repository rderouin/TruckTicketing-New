using System;

namespace SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;

public interface IServiceBusReEnqueueStrategyResult
{
    public ReEnqueueState ReEnqueueState { get; }

    public DateTimeOffset? ScheduledEnqueueTime { get; }
}
