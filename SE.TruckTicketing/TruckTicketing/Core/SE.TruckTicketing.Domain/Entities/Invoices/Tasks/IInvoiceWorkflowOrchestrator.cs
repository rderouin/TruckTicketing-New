using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Invoices;

namespace SE.TruckTicketing.Domain.Entities.Invoices.Tasks;

public interface IInvoiceWorkflowOrchestrator
{
    Task<InvoiceEntity> VoidInvoice(InvoiceEntity invoice);
}
