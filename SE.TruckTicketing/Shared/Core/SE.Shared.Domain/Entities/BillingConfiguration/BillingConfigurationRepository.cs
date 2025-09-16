using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.BillingConfiguration;

public class BillingConfigurationRepository : CosmosEFCoreSearchRepositoryBase<BillingConfigurationEntity>
{
    private const string ActiveBillingConfigurations = nameof(ActiveBillingConfigurations);

    public BillingConfigurationRepository(ISearchResultsBuilder resultsBuilder, ISearchQueryBuilder queryBuilder, IAbstractContextFactory abstractContextFactory, IQueryableHelper queryableHelper) :
        base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }

    [ExcludeFromCodeCoverage(Justification = "The implementation is offloaded into a separate method.")]
    protected override IQueryable<T> ApplyKeywordSearch<T>(IQueryable<T> source, string keywords)
    {
        return ApplyKeywordSearchImpl(source, keywords);
    }

    protected override IQueryable<T> ApplyFilter<T>(IQueryable<T> source, SearchCriteria criteria, IContext context)
    {
        if (source is not IQueryable<BillingConfigurationEntity> typedSource)
        {
            return base.ApplyFilter(source, criteria, context);
        }

        if (!criteria.Filters.ContainsKey(ActiveBillingConfigurations))
        {
            return base.ApplyFilter(source, criteria, context);
        }

        criteria.Filters.Remove(ActiveBillingConfigurations, out var loadActiveBillingConfigurations);
        var query = base.ApplyFilter(typedSource, criteria, context);

        foreach (var status in ((IEnumerable)loadActiveBillingConfigurations)!)
        {
            var currentDate = DateTime.Today;
            int.TryParse(status?.ToString(), out var isActive);
            switch (isActive)
            {
                case 1:
                    query = query.Where(billingConfig => (billingConfig.StartDate == null || billingConfig.StartDate < currentDate) &&
                                                         (billingConfig.EndDate == null || billingConfig.EndDate > currentDate));

                    break;
                case 2:
                    query = query.Where(billingConfig => billingConfig.StartDate > currentDate || billingConfig.EndDate < currentDate);
                    break;
            }
        }

        return (IQueryable<T>)query;
    }

    public IQueryable<T> ApplyKeywordSearchImpl<T>(IQueryable<T> source, string keywords)
    {
        // ensure a proper type
        if (source is not IQueryable<BillingConfigurationEntity> typedSource)
        {
            return source;
        }

        // apply filters
        if (keywords.HasText())
        {
            var value = keywords!.ToLower();
            typedSource = typedSource.Where(e => e.CustomerGeneratorName.ToLower().Contains(value) ||
                                                 e.Name.ToLower().Contains(value) ||
                                                 e.BillingContactName.ToLower().Contains(value));
        }

        return (IQueryable<T>)typedSource;
    }
}
