using System.ComponentModel;

namespace SE.BillingService.Contracts.Api.Enums;

public enum MessageAdapterPollingStrategy
{
    [Description("")]
    Undefined,

    [Description("Open Invoice")]
    OpenInvoice,
}
