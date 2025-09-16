using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.Sequences;

using Trident.Business;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.Invoices.Tasks;

public class InvoiceSequenceNumberGeneratorWorkflowTask : WorkflowTaskBase<BusinessContext<InvoiceEntity>>
{
    private readonly ISequenceNumberGenerator _sequenceNumberGenerator;

    public InvoiceSequenceNumberGeneratorWorkflowTask(ISequenceNumberGenerator sequenceNumberGenerator)
    {
        _sequenceNumberGenerator = sequenceNumberGenerator;
    }

    // Ensure this is one of the first work-flow tasks to run.
    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.BeforeInsert;

    public override async Task<bool> Run(BusinessContext<InvoiceEntity> context)
    {
        var invoiceNumber = await _sequenceNumberGenerator.GenerateSequenceNumbers(SequenceTypes.InvoiceProposal, context.Target.SiteId, 1).FirstAsync();
        context.Target.ProformaInvoiceNumber = invoiceNumber;
        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<InvoiceEntity> context)
    {
        var shouldRun = context.Operation == Operation.Insert && !context.Target.ProformaInvoiceNumber.HasText();
        return Task.FromResult(shouldRun);
    }
}
