using System;
using System.Collections.Generic;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders;

public sealed class EncodedInvoice : IDisposable
{
    public List<EncodedInvoicePart> Parts { get; set; }

    public void Dispose()
    {
        foreach (var part in Parts)
        {
            part?.Dispose();
        }
    }
}
