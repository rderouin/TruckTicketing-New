using System;
using System.IO;

using Newtonsoft.Json.Linq;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders;

public sealed class EncodedInvoicePart : IDisposable
{
    public Stream DataStream { get; set; }

    public string ContentType { get; set; }

    public bool IsAttachment { get; set; }

    public JObject Source { get; set; }

    public string PreferredFileName { get; set; }

    public void Dispose()
    {
        DataStream?.Dispose();
    }
}
