using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Sequences;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.LoadConfirmation.Tasks;

public class LoadConfirmationSequenceNumberGeneratorWorkflowTask : WorkflowTaskBase<BusinessContext<LoadConfirmationEntity>>
{
    private readonly ISequenceNumberGenerator _sequenceNumberGenerator;

    public LoadConfirmationSequenceNumberGeneratorWorkflowTask(ISequenceNumberGenerator sequenceNumberGenerator)
    {
        _sequenceNumberGenerator = sequenceNumberGenerator;
    }

    // Ensure this is one of the first work-flow tasks to run.
    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.BeforeInsert;

    public override async Task<bool> Run(BusinessContext<LoadConfirmationEntity> context)
    {
        var lcNumber = await _sequenceNumberGenerator.GenerateSequenceNumbers(SequenceTypes.LoadConfirmation, context.Target.SiteId, 1).FirstAsync();
        context.Target.Number = lcNumber;
        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<LoadConfirmationEntity> context)
    {
        var shouldRun = context.Operation == Operation.Insert && !context.Target.Number.HasText();
        return Task.FromResult(shouldRun);
    }
}
