using SE.Shared.Domain.Entities.Invoices;

namespace SE.TruckTicketing.Domain.Entities.Invoices.InvoiceReversal;

public class ReverseInvoiceInfo
{
    public InvoiceEntity OriginalInvoice { get; set; }

    public InvoiceEntity ReversalInvoice { get; set; }

    public InvoiceEntity ProformaInvoice { get; set; }

    public string ErrorMessage { get; set; }
}
