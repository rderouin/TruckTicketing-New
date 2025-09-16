using System;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class TruckTicketStubCreationRequest : GuidApiModelBase
{
    public Guid FacilityId { get; set; }

    public string SiteId { get; set; }

    public int Count { get; set; } = 1;

    public bool GeneratePdf { get; set; }
}
