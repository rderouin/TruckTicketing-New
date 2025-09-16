using System;

using Newtonsoft.Json;

namespace SE.Enterprise.Contracts.Models;

public class AttachmentModel
{
    [JsonProperty("Id")]
    public Guid Id { get; set; }

    [JsonProperty("SalesLineId")]
    public Guid SalesLineId { get; set; }

    [JsonProperty("SalesLineNumber")]
    public string SalesLineNumber { get; set; }

    [JsonProperty("TruckTicketId")]
    public Guid? TruckTicketId { get; set; }

    [JsonProperty("TruckTicketNumber")]
    public string TruckTicketNumber { get; set; }
}
