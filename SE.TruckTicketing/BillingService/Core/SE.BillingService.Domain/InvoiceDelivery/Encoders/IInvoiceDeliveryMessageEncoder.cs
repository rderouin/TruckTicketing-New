using System.Threading.Tasks;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Context;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders;

public interface IInvoiceDeliveryMessageEncoder
{
    MessageAdapterType SupportedMessageAdapterType { get; }

    Task<EncodedInvoice> EncodeMessage(InvoiceDeliveryContext context);
}
