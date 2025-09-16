using System.ComponentModel;

namespace SE.BillingService.Contracts.Api.Enums;

public enum InvoiceDeliveryTransportType
{
    Undefined = default,

    [Description("HTTP")]
    Http,

    [Description("SFTP")]
    Sftp,

    [Description("SMTP")]
    Smtp,
}
