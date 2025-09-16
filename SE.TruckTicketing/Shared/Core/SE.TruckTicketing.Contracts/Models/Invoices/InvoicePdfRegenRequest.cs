using System.Collections.Generic;

namespace SE.TruckTicketing.Contracts.Models.Invoices;

public class InvoicePdfRegenRequest
{
    public ICollection<Invoice> Invoices { get; set; }

    public bool IncludeInvoiceCopyWatermark { get; set; }

    public bool ShowRevisionNumber { get; set; }
}
