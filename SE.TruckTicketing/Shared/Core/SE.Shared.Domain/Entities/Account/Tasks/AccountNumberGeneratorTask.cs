using System.Threading.Tasks;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Sequences;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Account.Tasks;

public class AccountNumberGeneratorTask : WorkflowTaskBase<BusinessContext<AccountEntity>>
{
    private readonly ISequenceNumberGenerator _sequenceNumberGenerator;

    public AccountNumberGeneratorTask(ISequenceNumberGenerator sequenceNumberGenerator)
    {
        _sequenceNumberGenerator = sequenceNumberGenerator;
    }

    public override int RunOrder => 40;

    public override OperationStage Stage => OperationStage.BeforeInsert;

    public override async Task<bool> Run(BusinessContext<AccountEntity> context)
    {
        await foreach (var generatedSequence in _sequenceNumberGenerator.GenerateSequenceNumbers(SequenceTypes.AccountNumber, "A", 1))
        {
            context.Target.AccountNumber = generatedSequence;
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<AccountEntity> context)
    {
        return Task.FromResult(context.Operation == Operation.Insert && !context.Target.AccountNumber.HasText());
    }
}
