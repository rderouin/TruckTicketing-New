using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum InvoiceReversalReason
{
    Unspecified = default,

    [Description("Duplicate")]
    Duplicate,

    [Description("Entered in Error")]
    Error,

    [Description("Pricing")]
    Pricing,

    [Description("Billing Info")]
    BillingInfo,

    [Description("Generator Info")]
    GeneratorInfo,

    [Description("Other")]
    Other,
}
