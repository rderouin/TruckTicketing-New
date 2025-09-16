using System.Threading.Tasks;

using SE.BillingService.Domain.InvoiceDelivery.Context;

namespace SE.BillingService.Domain.InvoiceDelivery;

public interface IInvoiceDeliveryOrchestrator
{
    Task ProcessRequest(InvoiceDeliveryContext context);
}
