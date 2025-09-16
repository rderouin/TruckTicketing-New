using System.Linq;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public class TruckTicketTareWeightRepository : CosmosEFCoreSearchRepositoryBase<TruckTicketTareWeightEntity>
{
    public TruckTicketTareWeightRepository(ISearchResultsBuilder resultsBuilder,
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

        var typedSource = (IQueryable<TruckTicketTareWeightEntity>)source;

        return (IQueryable<T>)typedSource
           .Where(x => (x.TruckingCompanyName != null && x.TruckingCompanyName.ToLower().Contains(keywords.ToLower()))
                    || (x.TruckNumber != null && x.TruckNumber.ToLower().Contains(keywords.ToLower()))
                    || (x.TrailerNumber != null && x.TrailerNumber.ToLower().Contains(keywords.ToLower()))
                    || (x.TicketNumber != null && x.TicketNumber.ToLower().Contains(keywords.ToLower()))
                    || (x.CreatedBy != null && x.CreatedBy.ToLower().Contains(keywords.ToLower())));
    }
}
