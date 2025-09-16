using System.Threading.Tasks;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Tasks;

public class PopulateSearchableIdTask<TEntity> : WorkflowTaskBase<BusinessContext<TEntity>> where TEntity : TTEntityBase, ITTSearchableIdBase
{
    // Ensure this is one of the first work-flow tasks to run.
    public override int RunOrder => -3;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate | OperationStage.Custom;

    public override Task<bool> Run(BusinessContext<TEntity> context)
    {
        context.Target.SearchableId = context.Target.Id.ToString();

        return Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<TEntity> context)
    {
        return Task.FromResult(true);
    }
}
