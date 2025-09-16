using System.Linq;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public class TruckTicketWellClassificationRepository : CosmosEFCoreSearchRepositoryBase<TruckTicketWellClassificationUsageEntity>
{
    public TruckTicketWellClassificationRepository(ISearchResultsBuilder resultsBuilder,
                                                   ISearchQueryBuilder queryBuilder,
                                                   IAbstractContextFactory abstractContextFactory,
                                                   IQueryableHelper queryableHelper) :
        base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }

    protected override IQueryable<T> ApplyKeywordSearch<T>(IQueryable<T> source, string keywords)
    {
        if (string.IsNullOrEmpty(keywords))
        {
            return source;
        }

        var typedSource = (IQueryable<TruckTicketWellClassificationUsageEntity>)source;

        return (IQueryable<T>)typedSource
           .Where(x => x.FacilityName.ToLower().Contains(keywords));
    }
}
