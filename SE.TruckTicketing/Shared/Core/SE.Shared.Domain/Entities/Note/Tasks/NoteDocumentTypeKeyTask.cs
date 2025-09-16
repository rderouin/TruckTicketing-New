using System.Threading.Tasks;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Note.Tasks;

public class NoteDocumentTypeKeyTask : WorkflowTaskBase<BusinessContext<NoteEntity>>
{
    public override int RunOrder => 1;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override Task<bool> Run(BusinessContext<NoteEntity> context)
    {
        context.Target.InitPartitionKey();
        return Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<NoteEntity> context)
    {
        return Task.FromResult(true);
    }
}
