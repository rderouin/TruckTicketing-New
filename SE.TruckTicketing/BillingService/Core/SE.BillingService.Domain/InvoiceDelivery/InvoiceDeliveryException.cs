using System;
using System.Runtime.Serialization;

using JetBrains.Annotations;

namespace SE.BillingService.Domain.InvoiceDelivery;

public class InvoiceDeliveryException : Exception
{
    public InvoiceDeliveryException()
    {
    }

    protected InvoiceDeliveryException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public InvoiceDeliveryException([CanBeNull] string message) : base(message)
    {
    }

    public InvoiceDeliveryException([CanBeNull] string message, [CanBeNull] Exception innerException) : base(message, innerException)
    {
    }

    public string AdditionalMessage { get; set; } = string.Empty;
}
