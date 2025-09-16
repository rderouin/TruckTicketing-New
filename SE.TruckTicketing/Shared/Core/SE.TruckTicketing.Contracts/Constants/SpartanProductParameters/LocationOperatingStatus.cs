using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;

public enum LocationOperatingStatus
{
    Blank = default,

    [Description("Drilling")]
    Drilling = 1,

    [Description("Completions")]
    Completions = 2,

    [Description("Production")]
    Production = 3,
}
