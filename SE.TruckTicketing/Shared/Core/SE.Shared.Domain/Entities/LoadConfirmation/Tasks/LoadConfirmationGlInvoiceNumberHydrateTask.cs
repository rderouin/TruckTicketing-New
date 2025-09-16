using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Invoices;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.LoadConfirmation.Tasks;

public class LoadConfirmationGlInvoiceNumberHydrateTask : WorkflowTaskBase<BusinessContext<LoadConfirmationEntity>>
{
    private readonly IProvider<Guid, InvoiceEntity> _invoiceProvider;

    public LoadConfirmationGlInvoiceNumberHydrateTask(IProvider<Guid, InvoiceEntity> invoiceProvider)
    {
        _invoiceProvider = invoiceProvider;
    }

    // Ensure this is one of the first work-flow tasks to run.
    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<LoadConfirmationEntity> context)
    {
        var targetEntity = context.Target;
        var invoiceEntity = await _invoiceProvider.GetById(targetEntity.InvoiceId);
        if (invoiceEntity == null)
        {
            return true;
        }

        context.Target.GlInvoiceNumber = invoiceEntity.GlInvoiceNumber;

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<LoadConfirmationEntity> context)
    {
        var shouldRun = context.Original?.InvoiceId != context.Target.InvoiceId && context.Target.InvoiceId != default;
        return Task.FromResult(shouldRun);
    }
}
