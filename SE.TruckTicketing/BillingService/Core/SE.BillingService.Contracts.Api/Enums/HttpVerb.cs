using System.ComponentModel;

namespace SE.BillingService.Contracts.Api.Enums;

public enum HttpVerb
{
    Undefined = default,

    [Description("POST")]
    Post,

    [Description("PUT")]
    Put,

    [Description("PATCH")]
    Patch,
}
