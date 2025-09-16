using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Messaging.ServiceBus;

namespace SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;

public interface IServiceBusMessageEnqueuer
{
    Task<bool> ReEnqueue(IServiceBus serviceBus,
                         string topicName,
                         ServiceBusReceivedMessage serviceBusReceivedMessage,
                         ReEnqueueOptions reEnqueueOptions,
                         CancellationToken cancellationToken = default);

    Task<bool> ReEnqueue(IServiceBus serviceBus,
                         string topicName,
                         string originalMessage,
                         string applicationPropertiesJson,
                         ReEnqueueOptions reEnqueueOptions,
                         Dictionary<string, string> additionalMetadata = null,
                         string sessionId = null,
                         CancellationToken cancellationToken = default);
}
