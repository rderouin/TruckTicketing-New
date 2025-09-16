using System;
using System.Collections.Generic;

using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class FSTWorkTicketRequest
{
    public Guid FacilityId { get; set; }

    public string FacilityName { get; set; }

    public Guid LegalEntityId { get; set; }

    public string LegalEntityName { get; set; }

    public Guid ServiceTypeId { get; set; }

    public List<Guid> ServiceTypeIds { get; set; } = new();

    public string ServiceTypeName { get; set; }

    public DateTimeOffset? FromDate { get; set; }

    public DateTimeOffset? ToDate { get; set; }

    public List<Guid> TicketIds { get; set; } = new();

    public string Destination { get; set; }

    public List<TruckTicketStatus> SelectedTicketStatuses = new();

    public string RequestedFileType { get; set; }
}
