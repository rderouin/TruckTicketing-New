using System;

namespace SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;

public static class ReEnqueueConstants
{
    public const BackoffTypeEnum DefaultBackoffType = BackoffTypeEnum.Linear;

    public const int DefaultReEnqueueCount = 3;

    public const int MaxReEnqueueCount = 100;

    public const double DefaultExponentialFactor = 2.0;

    public static readonly TimeSpan DefaultDelay = TimeSpan.FromMinutes(5);

    public static class Keys
    {
        public const string ReEnqueueStrategy = nameof(ReEnqueueStrategy);
    }
}
