using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum InvoiceStatus
{
    [Description("Unknown")]
    Unknown = default,

    [Description("Posted")]
    Posted,

    [Description("UnPosted")]
    UnPosted,

    [Description("Aging/UnSent")]
    AgingUnSent,

    [Description("Posted/Rejected")]
    PostedRejected,

    [Description("Paid/UnSettled")]
    PaidUnSettled,

    [Description("Paid/Partial")]
    PaidPartial,

    [Description("Paid/Settled")]
    PaidSettled,

    [Description("Void")]
    Void,
}
