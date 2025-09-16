using System.Collections.Generic;
using System.Linq;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Invoices;

namespace SE.TruckTicketing.Client.Components.InvoiceComponents;

public class InvoiceNotesViewModel
{
    public InvoiceNotesViewModel(IList<Invoice> selectedInvoices)
    {
        SelectedInvoices = selectedInvoices;
        
        if(selectedInvoices.Any() && selectedInvoices.Count == 1)
        {
            var invoice = selectedInvoices.First();
            CollectionOwner = invoice.CollectionOwner;
            CollectionReason = invoice.CollectionReason;
            CollectionReasonComment = invoice.CollectionNotes;
        }
    }

    public IList<Invoice> SelectedInvoices { get; set; }

    public InvoiceCollectionOwners CollectionOwner { get; set; }

    public InvoiceCollectionReason CollectionReason { get; set; }

    public string CollectionReasonComment { get; set; }
}
