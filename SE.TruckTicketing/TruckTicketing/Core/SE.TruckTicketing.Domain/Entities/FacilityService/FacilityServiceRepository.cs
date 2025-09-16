using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.FacilityService;

public class FacilityServiceRepository : CosmosEFCoreSearchRepositoryBase<FacilityServiceEntity>
{
    public FacilityServiceRepository(ISearchResultsBuilder resultsBuilder, ISearchQueryBuilder queryBuilder, IAbstractContextFactory abstractContextFactory, IQueryableHelper queryableHelper) :
        base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }

    [ExcludeFromCodeCoverage(Justification = "The implementation is offloaded into a separate method.")]
    protected override IQueryable<T> ApplyKeywordSearch<T>(IQueryable<T> source, string keywords)
    {
        return ApplyKeywordSearchImpl(source, keywords);
    }

    public IQueryable<T> ApplyKeywordSearchImpl<T>(IQueryable<T> source, string keywords)
    {
        // ensure a proper type
        if (source is not IQueryable<FacilityServiceEntity> typedSource)
        {
            return source;
        }

        // apply filters
        if (keywords.HasText())
        {
            var value = keywords!.ToLower();
            typedSource = typedSource.Where(e => e.FacilityServiceNumber.ToLower().Contains(keywords.ToLower()) || e.ServiceTypeName.ToLower().Contains(keywords.ToLower()));
        }

        return (IQueryable<T>)typedSource;
    }
}
