using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Azure.Messaging.ServiceBus;

namespace SE.TridentContrib.Extensions.Azure.ServiceBus;

public interface IServiceBus
{
    Task Enqueue<T>(string topicName,
                    T item,
                    Dictionary<string, string> metadata,
                    string sessionId = null,
                    CancellationToken cancellationToken = default);

    Task Enqueue(string topicName,
                 ServiceBusMessage serviceBusMessage,
                 CancellationToken cancellationToken = default);

    Task EnqueueDelayed<T>(string topicName,
                           T item,
                           Dictionary<string, string> metadata,
                           DateTimeOffset scheduledEnqueueTime,
                           string sessionId = null,
                           CancellationToken cancellationToken = default);
}
