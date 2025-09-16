using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum CountryCode
{
    Undefined = default,

    [Description("Canada")]
    CA = 1,

    [Description("United States")]
    US = 2,
}

public enum SubstanceThresholdType
{
    Undefined = default,

    [Description("Percentage")]
    Percentage = 1,

    [Description("Fixed")]
    Fixed = 2,
}

public enum SubstanceThresholdFixedUnit
{
    Undefined = default,

    [Description("cubic meters")]
    M3 = 1,

    [Description("barrels")]
    Barrels = 2,
}

public enum ReportAsCutTypes
{
    Undefined = default,

    [Description("Water")]
    Water = 1,

    [Description("Oil")]
    Oil = 2,

    [Description("Solids")]
    Solids = 3,

    [Description("Service")]
    Service = 4,

    [Description("As per cuts entered")]
    AsPerCutsEntered = 5,
}
