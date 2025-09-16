using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum TruckTicketSource
{
    Undefined = default,

    [Description("Spartan")]
    Spartan = 1,

    [Description("Scanned")]
    Scanned = 2,

    [Description("Scaled")]
    Scaled = 3,

    [Description("Manual")]
    Manual = 4,
}
