using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Messaging.ServiceBus;

using Newtonsoft.Json;

namespace SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;

public class ServiceBusMessageEnqueuer : IServiceBusMessageEnqueuer
{
    private readonly IServiceBusReEnqueueStrategyFactory _serviceBusReEnqueueStrategyFactory;

    public ServiceBusMessageEnqueuer(IServiceBusReEnqueueStrategyFactory serviceBusReEnqueueStrategyFactory)
    {
        _serviceBusReEnqueueStrategyFactory = serviceBusReEnqueueStrategyFactory;
    }

    public async Task<bool> ReEnqueue(IServiceBus serviceBus,
                                      string topicName,
                                      ServiceBusReceivedMessage serviceBusReceivedMessage,
                                      ReEnqueueOptions reEnqueueOptions,
                                      CancellationToken cancellationToken = default)
    {
        var serviceBusMessage = new ServiceBusMessage(serviceBusReceivedMessage);
        var applicationProperties = serviceBusMessage.ApplicationProperties;

        var serviceBusReEnqueueStrategyResult = RunServiceBusReEnqueueStrategy(applicationProperties, reEnqueueOptions);

        if (!serviceBusReEnqueueStrategyResult.ScheduledEnqueueTime.HasValue)
        {
            return false;
        }

        serviceBusMessage.ScheduledEnqueueTime = serviceBusReEnqueueStrategyResult.ScheduledEnqueueTime.Value;
        applicationProperties[ReEnqueueConstants.Keys.ReEnqueueStrategy] = JsonConvert.SerializeObject(serviceBusReEnqueueStrategyResult.ReEnqueueState);

        await serviceBus.Enqueue(topicName, serviceBusMessage, cancellationToken);

        return true;
    }

    public async Task<bool> ReEnqueue(IServiceBus serviceBus,
                                      string topicName,
                                      string originalMessage,
                                      string applicationPropertiesJson,
                                      ReEnqueueOptions reEnqueueOptions,
                                      Dictionary<string, string> additionalMetadata = null,
                                      string sessionId = null,
                                      CancellationToken cancellationToken = default)
    {
        var applicationProperties = JsonConvert.DeserializeObject<Dictionary<string, string>>(applicationPropertiesJson);

        var serviceBusReEnqueueStrategyResult = RunServiceBusReEnqueueStrategy(applicationProperties, reEnqueueOptions);

        if (!serviceBusReEnqueueStrategyResult.ScheduledEnqueueTime.HasValue)
        {
            return false;
        }

        applicationProperties[ReEnqueueConstants.Keys.ReEnqueueStrategy] = JsonConvert.SerializeObject(serviceBusReEnqueueStrategyResult.ReEnqueueState);

        await serviceBus.EnqueueDelayed(topicName,
                             originalMessage,
                             applicationProperties,
                             serviceBusReEnqueueStrategyResult.ScheduledEnqueueTime.Value,
                             sessionId,
                             cancellationToken);

        return true;
    }

    private IServiceBusReEnqueueStrategyResult RunServiceBusReEnqueueStrategy<T>(IDictionary<string, T> applicationProperties, ReEnqueueOptions reEnqueueOptions)
        where T : class
    {
        ReEnqueueState reEnqueueState = null;

        if (applicationProperties.ContainsKey(ReEnqueueConstants.Keys.ReEnqueueStrategy) == true)
        {
            reEnqueueState = JsonConvert.DeserializeObject<ReEnqueueState>(applicationProperties[ReEnqueueConstants.Keys.ReEnqueueStrategy] as string);
        }

        var serviceBusReEnqueueStrategy = _serviceBusReEnqueueStrategyFactory.Create(reEnqueueOptions, reEnqueueState);

        var serviceBusReEnqueueStrategyResult = serviceBusReEnqueueStrategy.ScheduleNext();

        if (serviceBusReEnqueueStrategy.ScheduledEnqueueTime.HasValue)
        {
            applicationProperties[ReEnqueueConstants.Keys.ReEnqueueStrategy] = JsonConvert.SerializeObject(serviceBusReEnqueueStrategy.ReEnqueueState) as T;
        }

        applicationProperties[ReEnqueueConstants.Keys.ReEnqueueStrategy] = JsonConvert.SerializeObject(serviceBusReEnqueueStrategy.ReEnqueueState) as T;

        return serviceBusReEnqueueStrategyResult;
    }
}
