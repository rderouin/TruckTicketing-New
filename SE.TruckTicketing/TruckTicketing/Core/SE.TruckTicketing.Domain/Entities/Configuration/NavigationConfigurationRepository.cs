using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.Configuration;

public class NavigationConfigurationRepository : CosmosEFCoreSearchRepositoryBase<NavigationConfigurationEntity>
{
    public NavigationConfigurationRepository(ISearchResultsBuilder resultsBuilder,
                                             ISearchQueryBuilder queryBuilder,
                                             IAbstractContextFactory abstractContextFactory,
                                             IQueryableHelper queryableHelper)
        : base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }
}
