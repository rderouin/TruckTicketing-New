namespace SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;

public interface IServiceBusReEnqueueStrategyFactory
{
    IServiceBusReEnqueueStrategy Create(ReEnqueueOptions reEnqueueOptions, ReEnqueueState reEnqueueState);
}
