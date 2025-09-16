using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.BillingConfigurations.Tasks;

public class MatchPredicateUniqueConstraintCheckerTask : WorkflowTaskBase<BusinessContext<BillingConfigurationEntity>>
{
    private readonly IMatchPredicateManager _matchPredicateManager;

    public MatchPredicateUniqueConstraintCheckerTask(IMatchPredicateManager matchPredicateManager)
    {
        _matchPredicateManager = matchPredicateManager;
    }

    public override int RunOrder => 40;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<BillingConfigurationEntity> context)
    {
        //Create a target string to hash
        var entity = context.Target.Clone();
        var overlappingBillingConfigurations = await _matchPredicateManager.GetOverlappingBillingConfigurations(entity);

        var overlappingMatchPredicates = overlappingBillingConfigurations.SelectMany(x => x.MatchCriteria.Select(predicate => (predicate, billingConfig: x))).Where(x => x.predicate.IsEnabled
         && (x.predicate.StartDate == null || x.predicate.StartDate < DateTimeOffset.UtcNow) &&
            (x.predicate.EndDate == null || x.predicate.EndDate > DateTimeOffset.UtcNow)).ToList();

        var duplicateMatchPredicates = overlappingMatchPredicates.SelectMany(x => context.Target.MatchCriteria.Where(match => match.Hash == x.predicate.Hash)
                                                                                         .Select(predicate => (predicate, billingConfigs: x.billingConfig))).GroupBy(x => x.predicate.Id)
                                                                 .Select(y => y.First()).ToList();

        if (!duplicateMatchPredicates.Any())
        {
            context.Target.IsValid = true;
        }

        context.ContextBag.Add(BillingConfigurationWorkflowContextBagKeys.MatchPredicateHashIsUnique, !duplicateMatchPredicates.Any());
        if (duplicateMatchPredicates.Any())
        {
            context.ContextBag.Add(BillingConfigurationWorkflowContextBagKeys.DuplicateMatchPredicates, duplicateMatchPredicates);
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<BillingConfigurationEntity> context)
    {
        return Task.FromResult(context.Target is { IncludeForAutomation: true, IsDefaultConfiguration: false });
    }
}
