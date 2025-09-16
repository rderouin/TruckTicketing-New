using System;

namespace AzureServiceBusClientCli
{
    public class DataMigration
    {
        public string Message { get; set; }
        public string MessageType { get; set; }
        public DateTimeOffset GeneratedDate { get; set; }
        public bool Processed { get; set; }
        public string EntityId { get; set; }
        public string AxEntityId { get; set; }
    }
}
