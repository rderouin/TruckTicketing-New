using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Invoices;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.LoadConfirmation.Tasks;

public class LoadConfirmationInvoiceDocumentRegenerationTask : WorkflowTaskBase<BusinessContext<LoadConfirmationEntity>>
{
    private readonly IManager<Guid, InvoiceEntity> _invoiceManager;

    public LoadConfirmationInvoiceDocumentRegenerationTask(IManager<Guid, InvoiceEntity> invoiceManager)
    {
        _invoiceManager = invoiceManager;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<LoadConfirmationEntity> context)
    {
        var invoice = await _invoiceManager.GetById(context.Target.InvoiceId); // PK - TODO: ENTITY or INDEX

        if (invoice is { Status: InvoiceStatus.AgingUnSent, RequiresPdfRegeneration: false })
        {
            invoice.RequiresPdfRegeneration = true;
            await _invoiceManager.Save(invoice, true);
        }

        context.Target.RequiresInvoiceDocumentRegeneration = false;
        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<LoadConfirmationEntity> context)
    {
        var shouldRun = context.Operation is Operation.Update && context.Target.RequiresInvoiceDocumentRegeneration is true;
        return Task.FromResult(shouldRun);
    }
}
