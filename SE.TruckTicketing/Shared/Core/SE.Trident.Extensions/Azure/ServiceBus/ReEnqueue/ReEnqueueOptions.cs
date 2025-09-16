using System;
using System.ComponentModel.DataAnnotations;

namespace SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;

public class ReEnqueueOptions
{
    [Range(1, ReEnqueueConstants.MaxReEnqueueCount)]
    public int MaxReEnqueueCount { get; set; } = ReEnqueueConstants.DefaultReEnqueueCount;

    public BackoffTypeEnum BackoffType { get; set; } = ReEnqueueConstants.DefaultBackoffType;

    [Range(typeof(TimeSpan), "00:00:00", "100.00:00:00")]
    public TimeSpan Delay { get; set; } = ReEnqueueConstants.DefaultDelay;

    [Range(typeof(TimeSpan), "00:00:00", "100.00:00:00")]
    public TimeSpan? MaxDelay { get; set; }

    [Range(typeof(double), "1", "10")]
    public double ExponentialFactor { get; set; } = ReEnqueueConstants.DefaultExponentialFactor;
}
