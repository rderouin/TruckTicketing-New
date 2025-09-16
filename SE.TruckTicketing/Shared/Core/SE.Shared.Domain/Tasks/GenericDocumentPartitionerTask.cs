using System;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Domain;
using Trident.Workflow;

namespace SE.Shared.Domain.Tasks;

public class GenericDocumentPartitionerTask<TEntity> : WorkflowTaskBase<BusinessContext<TEntity>> where TEntity : DocumentDbEntityBase<Guid>
{
    public override int RunOrder => -1000;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override Task<bool> Run(BusinessContext<TEntity> context)
    {
        if (context.Target is IHaveCompositePartitionKey iHaveCompositePartitionKey)
        {
            iHaveCompositePartitionKey.InitPartitionKey(context.Original?.DocumentType);
        }

        return Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<TEntity> context)
    {
        return Task.FromResult(context.Target is IHaveCompositePartitionKey);
    }
}
