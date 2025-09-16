namespace SE.Shared.Domain.Entities.BillingConfiguration.Tasks;

public static class BillingConfigurationWorkflowContextBagKeys
{
    public const string MatchPredicateHashIsUnique = nameof(BillingConfigurationWorkflowContextBagKeys) + nameof(MatchPredicateHashIsUnique);

    public const string DuplicateMatchPredicates = nameof(BillingConfigurationWorkflowContextBagKeys) + nameof(DuplicateMatchPredicates);
}
