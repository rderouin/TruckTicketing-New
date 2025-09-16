using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum SalesLineCutType
{
    Unspecified = default,

    [Description("Total")]
    Total,

    [Description("Oil")]
    Oil,

    [Description("Water")]
    Water,

    [Description("Solid")]
    Solid,

    [Description("AdditionalService")]
    AdditionalService,
}
