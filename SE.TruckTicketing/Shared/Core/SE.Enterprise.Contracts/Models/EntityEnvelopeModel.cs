using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace SE.Enterprise.Contracts.Models;

public class EntityEnvelopeModel<T>
{
    [JsonProperty("Source")]
    public string Source { get; set; }

    [JsonProperty("SourceId")]
    public string SourceId { get; set; }

    [JsonIgnore]
    public Guid EnterpriseId
    {
        get => Guid.TryParse(SourceId, out var sourceId) ? sourceId : default;
        set => SourceId = value.ToString();
    }

    [JsonProperty("MessageType")]
    public string MessageType { get; set; }

    [JsonProperty("Operation")]
    public string Operation { get; set; }

    [JsonProperty("CorrelationId")]
    public string CorrelationId { get; set; }

    [JsonProperty("MessageDate")]
    public DateTime MessageDate { get; set; }

    [JsonProperty("Blobs")]
    public List<BlobAttachment> Blobs { get; set; }

    [JsonProperty("Payload")]
    public T Payload { get; set; }
}

public class BlobAttachment
{
    [JsonProperty("ContainerName")]
    public string ContainerName { get; set; }

    [JsonProperty("BlobPath")]
    public string BlobPath { get; set; }

    [JsonProperty("ContentType")]
    public string ContentType { get; set; }

    [JsonProperty("Filename")]
    public string Filename { get; set; }
}

public class EntityEnvelopeModel : EntityEnvelopeModel<object>
{
}
