using System;

namespace AzureServiceBusClientCli
{
    internal class MessageBase
    {
        public string Source { get; set; }
        public Guid SourceId { get; set; }
        public string MessageType { get; set; }
        public string Operation { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTimeOffset MessageDate { get; set; }
        public string BusinessEventId { get; set; }
        public string Id { get; set; }

    }
}
