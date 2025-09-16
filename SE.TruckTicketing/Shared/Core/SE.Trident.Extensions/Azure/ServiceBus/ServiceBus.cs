using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

using Azure.Messaging.ServiceBus;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using SE.Shared.Common.Extensions;

namespace SE.TridentContrib.Extensions.Azure.ServiceBus;

public abstract class ServiceBus : IServiceBus
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Converters = new List<JsonConverter>
        {
            new StringEnumConverter(),
        },
    };

    private readonly Lazy<ServiceBusClient> _serviceBusClientLazy;

    protected ServiceBus(string connectionString)
    {
        _serviceBusClientLazy = new(() => new(connectionString));
    }

    private ServiceBusClient ServiceBusClient => _serviceBusClientLazy.Value;

    public async Task Enqueue<T>(string topicName,
                                 T item,
                                 Dictionary<string, string> metadata,
                                 string sessionId = null,
                                 CancellationToken cancellationToken = default)
    {
        await EnqueueCore(topicName, item, metadata, scheduledEnqueueTime: null, sessionId, cancellationToken);
    }

    public async Task Enqueue(string topicName,
                              ServiceBusMessage serviceBusMessage,
                              CancellationToken cancellationToken = default)
    {
        // send the message
        var sender = ServiceBusClient.CreateSender(topicName);
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public async Task EnqueueDelayed<T>(string topicName,
                                        T item,
                                        Dictionary<string, string> metadata,
                                        DateTimeOffset scheduledEnqueueTime,
                                        string sessionId = null,
                                        CancellationToken cancellationToken = default)
    {
        await EnqueueCore(topicName, item, metadata, scheduledEnqueueTime, sessionId, cancellationToken);
    }

    private async Task EnqueueCore<T>(string topicName,
                                      T item,
                                      Dictionary<string, string> metadata,
                                      DateTimeOffset? scheduledEnqueueTime,
                                      string sessionId = null,
                                      CancellationToken cancellationToken = default)
    {
        // make a payload
        var body = item as string ?? JsonConvert.SerializeObject(item, SerializerSettings);

        // create a SB message
        var serviceBusMessage = new ServiceBusMessage(body)
        {
            ContentType = MediaTypeNames.Application.Json,
        };

        // add metadata
        foreach (var kvp in metadata ?? new Dictionary<string, string>())
        {
            serviceBusMessage.ApplicationProperties[kvp.Key] = kvp.Value;
        }

        if (scheduledEnqueueTime.HasValue)
        {
            serviceBusMessage.ScheduledEnqueueTime = scheduledEnqueueTime.Value;
        }

        // AMQP: group-id
        if (sessionId.HasText())
        {
            serviceBusMessage.SessionId = sessionId;
        }

        // send the message
        var sender = ServiceBusClient.CreateSender(topicName);
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}
