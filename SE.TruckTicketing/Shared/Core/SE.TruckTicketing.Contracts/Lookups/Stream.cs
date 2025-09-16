using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum Stream
{
    Undefined = default,

    [Description("Landfill")]
    Landfill = 1,

    [Description("Pipeline")]
    Pipeline = 2,

    [Description("Terminalling")]
    Terminalling = 3,

    [Description("Treating")]
    Treating = 4,

    [Description("Waste")]
    Waste = 5,

    [Description("Water")]
    Water = 6,
}
