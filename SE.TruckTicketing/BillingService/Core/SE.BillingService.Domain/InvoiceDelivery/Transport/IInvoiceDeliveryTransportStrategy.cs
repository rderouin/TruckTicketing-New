using System.Threading.Tasks;

using SE.BillingService.Domain.InvoiceDelivery.Context;

namespace SE.BillingService.Domain.InvoiceDelivery.Transport;

public interface IInvoiceDeliveryTransportStrategy
{
    Task Send(InvoiceDeliveryContext context);
}
