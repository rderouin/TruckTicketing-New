using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.AdditionalServicesConfiguration;

public class AdditionalServicesConfigurationRepository : CosmosEFCoreSearchRepositoryBase<AdditionalServicesConfigurationEntity>
{
    public AdditionalServicesConfigurationRepository(ISearchResultsBuilder resultsBuilder,
                                                     ISearchQueryBuilder queryBuilder,
                                                     IAbstractContextFactory abstractContextFactory,
                                                     IQueryableHelper queryableHelper)
        : base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }

    [ExcludeFromCodeCoverage(Justification = "The implementation is offloaded into a separate method.")]
    protected override IQueryable<T> ApplyKeywordSearch<T>(IQueryable<T> source, string keywords)
    {
        return ApplyKeywordSearchImpl(source, keywords);
    }

    public IQueryable<T> ApplyKeywordSearchImpl<T>(IQueryable<T> source, string keywords)
    {
        if (source is not IQueryable<AdditionalServicesConfigurationEntity> typedSource)
        {
            return source;
        }

        if (keywords.HasText())
        {
            var value = keywords!.ToLower();
            typedSource = typedSource.Where(x => x.Name.ToLower().Contains(value) ||
                                                 x.CustomerName.ToLower().Contains(value) ||
                                                 x.FacilityName.ToLower().Contains(value) ||
                                                 x.SiteId.ToLower().Contains(value));
        }

        return (IQueryable<T>)typedSource;
    }
}
