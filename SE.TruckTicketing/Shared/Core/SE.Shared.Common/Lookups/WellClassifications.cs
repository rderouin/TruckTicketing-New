using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum WellClassifications
{
    Undefined = default,

    [Description("Drilling")]
    Drilling = 1,

    [Description("Completions")]
    Completions = 2,

    [Description("Production")]
    Production = 3,

    Industrial = 4,

    Oilfield = 5,

    Remediation = 6,

    Construction = 7,
}
