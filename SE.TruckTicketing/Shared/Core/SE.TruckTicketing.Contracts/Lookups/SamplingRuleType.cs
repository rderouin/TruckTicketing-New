using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum SamplingRuleType
{
    Unspecified = default,
    
    [Description("Weight")]
    Weight,
    
    [Description("Time")]
    Time,
    
    [Description("Load")]
    Load,
}
