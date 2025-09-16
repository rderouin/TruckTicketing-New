using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.Invoices.Tasks;

public class InvoiceGLNumberReferenceToLoadConfirmationTask : WorkflowTaskBase<BusinessContext<InvoiceEntity>>
{
    private readonly IProvider<Guid, LoadConfirmationEntity> _loadConfirmationProvider;

    public InvoiceGLNumberReferenceToLoadConfirmationTask(IProvider<Guid, LoadConfirmationEntity> loadConfirmationProvider)
    {
        _loadConfirmationProvider = loadConfirmationProvider;
    }

    // Ensure this is one of the first work-flow tasks to run.
    public override int RunOrder => 15;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<InvoiceEntity> context)
    {
        var associatedLoadConfirmations = (await _loadConfirmationProvider.Get(x => x.InvoiceId == context.Target.Id))?.ToList();
        if (associatedLoadConfirmations == null || !associatedLoadConfirmations.Any())
        {
            return true;
        }

        foreach (var associatedLoadConfirmation in associatedLoadConfirmations)
        {
            associatedLoadConfirmation.GlInvoiceNumber = context.Target.GlInvoiceNumber;
            await _loadConfirmationProvider.Update(associatedLoadConfirmation, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<InvoiceEntity> context)
    {
        var shouldRun = context.Original?.GlInvoiceNumber != context.Target.GlInvoiceNumber && context.Target.GlInvoiceNumber.HasText();
        return Task.FromResult(shouldRun);
    }
}
