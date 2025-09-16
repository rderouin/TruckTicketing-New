using System;
using System.Collections.Generic;

using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Sampling;

public class LandfillSamplingDto : GuidApiModelBase
{
    public Guid FacilityId { get; set; }
    
    public Guid SamplingRuleId { get; set; }
    
    public SamplingRuleType SamplingRuleType { get; set; }
    
    public string Threshold { get; set; }
    
    public string WarningThreshold { get; set; }
    
    public string Value { get; set; }
    
    public string ProductNumberFilter { get; set; }

    public List<string> WellClassificationFilters { get; set; } = new();

    public DateTime? TimeOfLastLoadSample { get; set; }
}
