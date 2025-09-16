namespace SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;

public class ServiceBusReEnqueueStrategyFactory : IServiceBusReEnqueueStrategyFactory
{
    public IServiceBusReEnqueueStrategy Create(ReEnqueueOptions reEnqueueOptions, ReEnqueueState reEnqueueState)
    {
        return ServiceBusReEnqueueStrategy.Create(reEnqueueOptions, reEnqueueState);
    }
}
