using System.Threading.Tasks;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;

namespace SE.BillingService.Domain.InvoiceDelivery.Transport;

public interface IInvoiceDeliveryTransport
{
    public InvoiceDeliveryTransportType TransportType { get; }

    Task Send(EncodedInvoicePart part, InvoiceDeliveryTransportInstructions instructions);
}
