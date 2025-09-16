using SE.BillingService.Contracts.Api.Enums;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders;

public interface IInvoiceDeliveryMessageEncoderSelector
{
    IInvoiceDeliveryMessageEncoder Select(MessageAdapterType messageAdapterType);
}
