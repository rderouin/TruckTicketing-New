using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

using SE.BillingService.Contracts.Api.Enums;

namespace SE.BillingService.Domain.InvoiceDelivery.Context;

public class InvoiceDeliveryTransportInstructions
{
    public Uri DestinationUri { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public X509Certificate2 Certificate { get; set; }

    public byte[] PrivateKey { get; set; }

    public HttpVerb HttpVerb { get; set; }

    public Dictionary<string, string> HttpHeaders { get; set; } = new();
}
