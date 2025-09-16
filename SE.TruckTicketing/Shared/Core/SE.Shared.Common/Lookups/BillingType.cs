using System.ComponentModel;

namespace SE.Shared.Common.Lookups;

public enum BillingType
{
    Undefined = default,

    [Description("Credit Card")]
    CreditCard = 1,

    [Description("EDI - Invoice/Ticket")]
    EDIInvoiceTicket = 2,

    [Description("Email")]
    Email = 3,

    [Description("Mail")]
    Mail = 4,
}
