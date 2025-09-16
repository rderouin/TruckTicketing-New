using System;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class TruckTicketTareWeight : GuidApiModelBase
{
    public string TruckingCompanyName { get; set; }

    public Guid? TruckingCompanyId { get; set; }

    public string TruckNumber { get; set; }

    public string TrailerNumber { get; set; }

    public DateTimeOffset LoadDate { get; set; }

    public Guid? FacilityId { get; set; }

    public string FacilityName { get; set; }

    public string FacilitySiteId { get; set; }

    public Guid TicketId { get; set; }

    public string TicketNumber { get; set; }

    public double TareWeight { get; set; }

    public bool IsActivated { get; set; } = true;

    public string CreatedBy { get; set; }
}
