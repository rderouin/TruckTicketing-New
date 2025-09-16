using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum InvoiceDistributionMethod
{
    [Description("Unknown")]
    Unknown = default,

    [Description("Credit Card")]
    CreditCard = 1,

    [Description("EDI")]
    EDI = 2,

    [Description("Email")]
    Email = 3,

    [Description("Mail")]
    Mail = 4,
}
