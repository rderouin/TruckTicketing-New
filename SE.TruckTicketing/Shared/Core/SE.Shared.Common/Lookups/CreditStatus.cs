using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum CreditStatus
{
    Undefined = default,

    [Description("New")]
    New = 1,

    [Description("Pending")]
    Pending = 2,

    [Description("Provisional Approval")]
    ProvisionalApproval = 3,

    [Description("Requires Renewal")]
    RequiresRenewal = 4,

    [Description("Approved")]
    Approved = 5,

    [Description("Denied")]
    Denied = 6,

    [Description("Closed")]
    Closed = 7,
}
