using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum HazardousClassification
{
    Undefined = default,

    [Description("Non-hazardous")]
    Nonhazardous = 1,

    [Description("Hazardous")]
    Hazardous = 2,
}

public enum SourceRegionEnum
{
    Undefined = default,

    [Description("In Region")]
    InRegion = 1,

    [Description("Out of Region")]
    OutOfRegion = 2,

    [Description("Out of Province")]
    OutOfProvince = 3,
}
