using System;
using System.Collections.Generic;
using System.Linq;

using Humanizer;

using Microsoft.AspNetCore.Components;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Contracts.Api.Models;

namespace SE.TruckTicketing.Client.Components.InvoiceExchange;

public partial class TransportSettings
{
    private static readonly Dictionary<InvoiceDeliveryTransportType, string> TransportTypes = Enum.GetValues<InvoiceDeliveryTransportType>()
                                                                                                  .Where(e => e != default)
                                                                                                  .ToDictionary(e => e, e => e.Humanize());

    private static readonly Dictionary<HttpVerb, string> HttpVerbs = Enum.GetValues<HttpVerb>()
                                                                         .Where(e => e != default)
                                                                         .ToDictionary(e => e, e => e.Humanize());

    [Parameter]
    public InvoiceExchangeTransportSettingsDto Config { get; set; } = new();

    [Parameter]
    public bool IsAttachmentSettings { get; set; }

    private bool IsPrivateKeySupported => Config.TransportType == InvoiceDeliveryTransportType.Http;

    private string SecretTitle =>
        Config.TransportType switch
        {
            InvoiceDeliveryTransportType.Http => "Certificate",
            InvoiceDeliveryTransportType.Sftp => "Private Key",
            _ => "Secret",
        };
}
