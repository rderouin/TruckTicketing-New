using System;

namespace AzureServiceBusClientCli
{
    public class DataMigrationMessage
    {
        public string Message { get; set; }
        public string MessageType { get; set; }
        public DateTimeOffset GeneratedDate { get; set; }
        public Guid GUID { get; set; }
        public Guid EntityId { get; set; }
        public string AxEntityId { get; set; }
        public bool Processed { get; set; }
        public DateTimeOffset ProcessedDate { get; set; }
        public string TopicName { get; set; }
        public long SequenceId { get;  set; }
    }
}
