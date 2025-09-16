using System;
using System.Collections.Generic;

using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Operations;
public class LandfillDailyReportRequest
{
    public List<Guid> FacilityIds { get; set; }

    public DateTimeOffset FromDate { get; set; }

    public DateTimeOffset ToDate { get; set; }

    public Class? SelectedClass { get; set; }

    public string RequestedFileType { get; set; }
}


