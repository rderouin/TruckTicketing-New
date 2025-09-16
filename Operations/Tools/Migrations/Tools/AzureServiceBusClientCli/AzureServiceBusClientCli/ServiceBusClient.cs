using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace AzureServiceBusClientCli
{
    internal class SBClient
    {
        private readonly string _serviceBusConnectionString;

        private ServiceBusClient ServiceBusClient { get; set; }
        public ServiceBusSender Sender { get; }

        public SBClient(string connectionString, string topicName)
        {
            this._serviceBusConnectionString = connectionString;
            ServiceBusClient = new ServiceBusClient(_serviceBusConnectionString
                , new ServiceBusClientOptions()
                {
                    TransportType = ServiceBusTransportType.AmqpWebSockets
                }

                );
            Sender = ServiceBusClient.CreateSender(topicName);
        }

        public async Task Send(string jsonContent, CancellationToken cancelToken)
        {
            var message = JsonConvert.DeserializeObject<MessageBase>(jsonContent);
            var metadata = new Dictionary<string, string>
            {
                { nameof(MessageBase.Source), message!.Source },
                { nameof(MessageBase.SourceId), $"{message.SourceId}" },
                { nameof(MessageBase.MessageType), message.MessageType },
                { nameof(MessageBase.Operation), message.Operation },
                { nameof(MessageBase.CorrelationId), $"{message.CorrelationId}" },
                { nameof(MessageBase.MessageDate), $"{message.MessageDate}" },
                { nameof(MessageBase.BusinessEventId), message.BusinessEventId },
                { nameof(MessageBase.Id), message.Id },
                
            };
            await this.Enqueue(jsonContent, metadata, "MigrationRun", cancelToken);

        }

        private async Task Enqueue<T>(T item, Dictionary<string, string> metadata, string sessionId = null, CancellationToken cancellationToken = default)
        {
            // make a payload
            var body = item as string ?? JsonConvert.SerializeObject(item);

            // create a SB message
            var serviceBusMessage = new ServiceBusMessage(body);

            foreach (var kvp in metadata)
            {
                serviceBusMessage.ApplicationProperties[kvp.Key] = kvp.Value;
            }

            // AMQP: group-id
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                serviceBusMessage.SessionId = sessionId;
            }

            // send the message
         
            await Sender.SendMessageAsync(serviceBusMessage, cancellationToken);
        }
    }
}
