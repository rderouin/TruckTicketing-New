using System;
using System.Collections.Generic;

namespace SE.TruckTicketing.Domain.Entities.Reports;

public class FSTWorkTicketParametersDataset
{
    public Guid FacilityId { get; set; }

    public string FacilityName { get; set; }

    public Guid LegalEntityId { get; set; }

    public string LegalEntityName { get; set; }

    public Guid ServiceTypeId { get; set; }

    public string ServiceTypeName { get; set; }

    public DateTimeOffset? FromDate { get; set; }

    public DateTimeOffset? ToDate { get; set; }

    public List<Guid> TicketIds { get; set; } = new();
}
