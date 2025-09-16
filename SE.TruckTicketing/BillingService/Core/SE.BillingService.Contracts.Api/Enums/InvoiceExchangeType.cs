using System.ComponentModel;

namespace SE.BillingService.Contracts.Api.Enums;

public enum InvoiceExchangeType
{
    Unknown = default,

    [Description("Global")]
    Global,

    [Description("Business Stream")]
    BusinessStream,

    [Description("Legal Entity")]
    LegalEntity,

    [Description("Customer")]
    Customer,
}
