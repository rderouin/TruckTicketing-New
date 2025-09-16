using System.Linq;

using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.ServiceType;

public class ServiceTypeRepository : CosmosEFCoreSearchRepositoryBase<ServiceTypeEntity>
{
    public ServiceTypeRepository(ISearchResultsBuilder resultsBuilder,
                                 ISearchQueryBuilder queryBuilder,
                                 IAbstractContextFactory abstractContextFactory,
                                 IQueryableHelper queryableHelper)
        : base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }

    protected override IQueryable<T> ApplyFilter<T>(IQueryable<T> source, SearchCriteria criteria, IContext context)
    {
        if (source is not IQueryable<ServiceTypeEntity> typedSource)
        {
            return base.ApplyFilter(source, criteria, context);
        }

        criteria.Filters.Remove("SearchByKeyword", out var keywordSearch);
        var query = base.ApplyFilter(typedSource, criteria, context);
        if (keywordSearch != null)
        {
            var ax = (Axiom)keywordSearch;
            var expression = ax.CreateExpression<ServiceTypeEntity>(true);
            query = query.Where(expression);
        }

        return (IQueryable<T>)query;
    }

    protected override IQueryable<T> ApplyKeywordSearch<T>(IQueryable<T> source, string keywords)
    {
        if (string.IsNullOrEmpty(keywords))
        {
            return source;
        }

        var typedSource = (IQueryable<ServiceTypeEntity>)source;
        var keyword = keywords.ToLower();

        return (IQueryable<T>)typedSource.Where(x => (x.ServiceTypeId != null && x.ServiceTypeId.ToLower().Contains(keyword)) ||
                                                     (x.Description != null && x.Description.ToLower().Contains(keyword))
                                                  || (x.OilItemName != null && x.OilItemName.ToLower().Contains(keyword)) ||
                                                     (x.WaterItemName != null && x.WaterItemName.ToLower().Contains(keyword))
                                                  || (x.SolidItemName != null && x.SolidItemName.ToLower().Contains(keyword)) ||
                                                     (x.TotalItemName != null && x.TotalItemName.ToLower().Contains(keyword)) ||
                                                     (x.LegalEntityCode != null && x.LegalEntityCode.ToLower().Contains(keyword)) ||
                                                     (x.CountryCodeString != null && x.CountryCodeString.ToLower().Contains(keyword)) ||
                                                     (x.Name != null && x.Name.ToLower().Contains(keyword)));
    }
}
