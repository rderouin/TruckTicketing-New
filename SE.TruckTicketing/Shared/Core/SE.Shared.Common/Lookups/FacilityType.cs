using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum FacilityType
{
    [Description("Unknown")]
    Unknown = 0,

    [Description("LF")]
    Lf = 1,

    [Description("FST")]
    Fst = 2,

    [Description("Cavern")]
    Cavern = 3,

    [Description("SWD")]
    Swd = 4,
}
