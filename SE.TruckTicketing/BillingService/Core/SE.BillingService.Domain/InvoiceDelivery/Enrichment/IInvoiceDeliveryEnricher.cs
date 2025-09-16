using System.Threading.Tasks;

using SE.BillingService.Domain.InvoiceDelivery.Context;

namespace SE.BillingService.Domain.InvoiceDelivery.Enrichment;

public interface IInvoiceDeliveryEnricher
{
    Task Enrich(InvoiceDeliveryContext context);
}
