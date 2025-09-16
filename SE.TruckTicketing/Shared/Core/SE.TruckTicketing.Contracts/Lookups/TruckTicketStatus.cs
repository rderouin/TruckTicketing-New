using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum TruckTicketStatus
{
    New = default,

    [Description("Open")]
    Open = 1,

    [Description("Approved")]
    Approved = 2,

    [Description("Void")]
    Void = 3,

    [Description("Hold")]
    Hold = 4,

    [Description("Invoiced")]
    Invoiced = 5,

    [Description("Stub")]
    Stub = 6,
}
