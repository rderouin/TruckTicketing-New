using System;

namespace SE.TruckTicketing.Contracts.Models.Acknowledgement;

public class Acknowledgement : GuidApiModelBase
{
    public Guid ReferenceEntityId { get; set; }

    public string Status { get; set; }

    public string AcknowledgedBy { get; set; }

    public DateTimeOffset? AcknowledgeAt { get; set; }
}
