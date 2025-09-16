using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum DowNonDow
{
    Undefined = default,

    Dow = 1,

    [Description("Non Dow")]
    NonDow = 2,

    Hazardous = 3,

    [Description("Non-Hazardous")]
    NonHazardous = 4
}
