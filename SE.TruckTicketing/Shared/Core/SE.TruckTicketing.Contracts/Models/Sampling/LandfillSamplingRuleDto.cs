using System.Collections.Generic;

using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Sampling;

public class LandfillSamplingRuleDto : GuidApiModelBase
{
    public SamplingRuleType SamplingRuleType { get; set; }
    
    public StateProvince Province { get; set; }
    
    public CountryCode CountryCode { get; set; }
    
    public string Threshold { get; set; }
    
    public string WarningThreshold { get; set; }

    public string ProductNumberFilter { get; set; }
    
    public List<string> WellClassificationFilters { get; set; } = new();
}
