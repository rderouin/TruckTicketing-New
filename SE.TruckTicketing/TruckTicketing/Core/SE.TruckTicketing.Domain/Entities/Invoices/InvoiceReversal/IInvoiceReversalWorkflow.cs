using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Invoices;

namespace SE.TruckTicketing.Domain.Entities.Invoices.InvoiceReversal;

public interface IInvoiceReversalWorkflow
{
    Task<ReverseInvoiceInfo> ReverseInvoice(ReverseInvoiceRequest reverseInvoiceRequest);
}
