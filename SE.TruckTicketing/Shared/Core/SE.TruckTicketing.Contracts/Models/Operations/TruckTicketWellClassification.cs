using System;

using SE.Shared.Common.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class TruckTicketWellClassification : GuidApiModelBase
{
    public Guid TruckTicketId { get; set; }

    public string TicketNumber { get; set; }

    public Guid SourceLocationId { get; set; }

    public string SourceLocationIdentifier { get; set; }

    public Guid FacilityId { get; set; }

    public string FacilityName { get; set; }

    public DateTimeOffset Date { get; set; }

    public WellClassifications WellClassification { get; set; }
}
