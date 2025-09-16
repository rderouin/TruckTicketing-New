using System.ComponentModel;

namespace SE.BillingService.Contracts.Api.Enums;

public enum MessageAdapterType
{
    [Description("Undefined")]
    Undefined = default,

    [Description("PIDX")]
    Pidx,

    [Description("CSV")]
    Csv,

    [Description("Open API")]
    OpenApi,

    [Description("HTTP Endpoint")]
    HttpEndpoint,

    [Description("Mail Message")]
    MailMessage,
}
