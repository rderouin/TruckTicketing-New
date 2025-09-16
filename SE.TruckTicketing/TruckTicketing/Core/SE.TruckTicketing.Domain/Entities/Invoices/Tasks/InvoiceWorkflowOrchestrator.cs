using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;

namespace SE.TruckTicketing.Domain.Entities.Invoices.Tasks;

public class InvoiceWorkflowOrchestrator : IInvoiceWorkflowOrchestrator
{
    private readonly IManager<Guid, InvoiceEntity> _invoiceManager;

    private readonly IManager<Guid, LoadConfirmationEntity> _loadConfirmationManager;

    public InvoiceWorkflowOrchestrator(IManager<Guid, InvoiceEntity> invoiceManager,
                                       IManager<Guid, LoadConfirmationEntity> loadConfirmationManager)
    {
        _invoiceManager = invoiceManager;
        _loadConfirmationManager = loadConfirmationManager;
    }

    public async Task<InvoiceEntity> VoidInvoice(InvoiceEntity invoice)
    {
        //Update LC Status to Void
        var loadConfirmations = (await _loadConfirmationManager.Get(lc => lc.InvoiceId == invoice.Id)).ToList(); // PK - XP for LC by Invoice ID
        foreach (var loadConfirmation in loadConfirmations)
        {
            if (invoice.Status is InvoiceStatus.Void && loadConfirmation.Status is not LoadConfirmationStatus.Void)
            {
                loadConfirmation.Status = LoadConfirmationStatus.Void;
            }

            loadConfirmation.InvoiceStatus = invoice.Status;
            await _loadConfirmationManager.Update(loadConfirmation);
        }

        //Update Invoice to Void
        return await _invoiceManager.Update(invoice);
    }
}
