namespace SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;

public interface IServiceBusReEnqueueStrategy : IServiceBusReEnqueueStrategyResult
{
    ReEnqueueOptions ReEnqueueOptions { get; }

    IServiceBusReEnqueueStrategyResult ScheduleNext();
}
