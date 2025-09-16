namespace SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;

public class ReEnqueueState
{
    public ReEnqueueState(ReEnqueueOptions reEnqueueOptions, int reEnqueueCount = 0)
    {
        ReEnqueueOptions = reEnqueueOptions;
        ReEnqueueCount = reEnqueueCount;
    }

    public ReEnqueueOptions ReEnqueueOptions { get; private set; }

    public int ReEnqueueCount { get; private set; }

    public void IncrementReEnqueueCount()
    {
        ++ReEnqueueCount;
    }
}
