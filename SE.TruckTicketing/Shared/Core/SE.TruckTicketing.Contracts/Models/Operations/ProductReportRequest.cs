using System;
using System.Collections.Generic;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class ProducerReportRequest
{
    public List<Guid> FacilityIds { get; set; } = new();

    public List<Guid> GeneratorIds { get; set; } = new();

    public List<string> FacilityNames { get; set; }

    public List<string> GeneratorNames { get; set; }

    public List<Guid> SourceLocationIds { get; set; } = new();

    public List<string> SourceLocationNames { get; set; }

    public List<Guid> FacilityServiceIds { get; set; } = new();

    public List<string> FacilityServiceNames { get; set; }

    public List<Guid> TruckingCompanyIds { get; set; } = new();

    public List<string> TruckingCompanyNames { get; set; }

    public Guid LegalEntityId { get; set; }

    public string LegalEntityName { get; set; }

    public List<Guid> ServiceTypeIds { get; set; } = new();

    public string ServiceTypeName { get; set; }

    public DateTimeOffset FromDate { get; set; }

    public DateTimeOffset ToDate { get; set; }

    public bool PriceOnLoad { get; set; }
}
