using System;
using System.Collections.Generic;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class LoadSummaryReportRequest
{
    public Guid FacilityId { get; set; }

    public string FacilityName { get; set; }

    public Guid GeneratorId { get; set; }

    public string GeneratorName { get; set; }

    public Guid SourceLocationId { get; set; }

    public string SourceLocationName { get; set; }

    public List<Guid> MaterialApprovalIds { get; set; } = new();

    public DateTimeOffset? FromDate { get; set; }

    public DateTimeOffset? ToDate { get; set; }

    public List<Guid> TruckingCompanyIds { get; set; } = new();

    public List<string> TruckingCompanyNames { get; set; } = new();

    public string LegalEntity { get; set; }

    public string SiteId { get; set; }

    public DateTime? ReportExecutionTime { get; set; }
}
