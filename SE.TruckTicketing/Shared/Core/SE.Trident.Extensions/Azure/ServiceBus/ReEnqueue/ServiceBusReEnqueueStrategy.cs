using System;
using System.ComponentModel;

using Trident.Extensions;

namespace SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;

public class ServiceBusReEnqueueStrategy : IServiceBusReEnqueueStrategy
{
    private ServiceBusReEnqueueStrategy(ReEnqueueOptions reEnqueueOptions, ReEnqueueState reEnqueueState)
    {
        reEnqueueOptions.GuardIsNotNull(nameof(reEnqueueOptions));

        ReEnqueueOptions = reEnqueueOptions;
        ReEnqueueState = reEnqueueState ?? new(reEnqueueOptions);
        ScheduledEnqueueTime = null;
    }

    public static ServiceBusReEnqueueStrategy Create(ReEnqueueOptions reEnqueueOptions, ReEnqueueState reEnqueueState)
    {
        return new(reEnqueueOptions, reEnqueueState);
    }

    public IServiceBusReEnqueueStrategyResult ScheduleNext()
    {
        var currentReEnqueueCount = ReEnqueueState.ReEnqueueCount;

        if (currentReEnqueueCount >= ReEnqueueOptions.MaxReEnqueueCount)
        {
            ScheduledEnqueueTime = null;

            return this;
        }

        ReEnqueueState.IncrementReEnqueueCount();

        var nextDelay = ComputeNextDelay(ReEnqueueOptions.BackoffType,
                                         currentReEnqueueCount,
                                         ReEnqueueOptions.Delay,
                                         ReEnqueueOptions.MaxDelay,
                                         ReEnqueueOptions.ExponentialFactor);

        ScheduledEnqueueTime = DateTimeOffset.UtcNow.Add(nextDelay);

        return this;
    }

    private static TimeSpan ComputeNextDelay(BackoffTypeEnum backoffType,
                                            int reEnqueueCount,
                                            TimeSpan currentDelay,
                                            TimeSpan? maxDelay,
                                            double exponentialFactor = ReEnqueueConstants.DefaultExponentialFactor)
    {
        var nextDelay = backoffType switch
        {
            BackoffTypeEnum.Constant => currentDelay,
            BackoffTypeEnum.Linear => TimeSpan.FromMilliseconds(reEnqueueCount * currentDelay.TotalMilliseconds),
            BackoffTypeEnum.Exponential => TimeSpan.FromMilliseconds(Math.Pow(exponentialFactor, reEnqueueCount) * currentDelay.TotalMilliseconds),
            _ => throw new InvalidEnumArgumentException(),
        };

        if (maxDelay.HasValue && nextDelay > maxDelay.Value)
        {
            nextDelay = maxDelay.Value;
        }

        return nextDelay;
    }

    public ReEnqueueOptions ReEnqueueOptions { get; }

    public ReEnqueueState ReEnqueueState { get; }

    public DateTimeOffset? ScheduledEnqueueTime { get; private set; }
}
