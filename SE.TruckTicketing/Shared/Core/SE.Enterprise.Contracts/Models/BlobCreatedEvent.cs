using System;

namespace SE.Enterprise.Contracts.Models;

public class BlobCreatedEvent
{
    public string Id { get; set; }

    public string Subject { get; set; }

    public string EventType { get; set; }

    public DateTime EventTime { get; set; }

    public BlobCreatedData Data { get; set; }

    public string Topic { get; set; }
}
