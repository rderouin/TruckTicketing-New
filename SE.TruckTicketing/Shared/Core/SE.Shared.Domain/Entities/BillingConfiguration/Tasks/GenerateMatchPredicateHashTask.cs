using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.BillingConfiguration.Tasks;

public class GenerateMatchPredicateHashTask : WorkflowTaskBase<BusinessContext<BillingConfigurationEntity>>
{
    public override int RunOrder => 30;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<BillingConfigurationEntity> context)
    {
        //create hash
        foreach (var matchPredicate in context.Target.MatchCriteria.Where(matchPredicate => matchPredicate.IsEnabled))
        {
            matchPredicate.ComputeHash();
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<BillingConfigurationEntity> context)
    {
        return Task.FromResult(context.Target is { IncludeForAutomation: true, IsDefaultConfiguration: false });
    }
}
