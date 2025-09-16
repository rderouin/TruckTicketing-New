using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum TruckTicketType
{
    Undefined = default,

    [Description("Scale")]
    LF = 1,

    [Description("Spartan")]
    SP = 2,

    [Description("Work")]
    WT = 3,
}
