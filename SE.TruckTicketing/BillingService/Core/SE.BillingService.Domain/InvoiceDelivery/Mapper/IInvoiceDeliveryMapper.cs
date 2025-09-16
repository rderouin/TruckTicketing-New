using System.Threading.Tasks;

using SE.BillingService.Domain.InvoiceDelivery.Context;

namespace SE.BillingService.Domain.InvoiceDelivery.Mapper;

public interface IInvoiceDeliveryMapper
{
    Task Map(InvoiceDeliveryContext context);
}
