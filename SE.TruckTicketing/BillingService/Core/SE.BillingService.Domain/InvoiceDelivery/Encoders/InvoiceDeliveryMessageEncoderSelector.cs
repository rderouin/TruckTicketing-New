using System;
using System.Collections.Generic;
using System.Linq;

using SE.BillingService.Contracts.Api.Enums;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders;

public class InvoiceDeliveryMessageEncoderSelector : IInvoiceDeliveryMessageEncoderSelector
{
    private readonly IEnumerable<IInvoiceDeliveryMessageEncoder> _encoders;

    public InvoiceDeliveryMessageEncoderSelector(IEnumerable<IInvoiceDeliveryMessageEncoder> encoders)
    {
        _encoders = encoders;
    }

    public IInvoiceDeliveryMessageEncoder Select(MessageAdapterType messageAdapterType)
    {
        var encoder = _encoders.FirstOrDefault(e => e.SupportedMessageAdapterType == messageAdapterType);
        if (encoder != null)
        {
            return encoder;
        }

        throw new NotSupportedException($"Encoder is not supported '{messageAdapterType}'.");
    }
}
