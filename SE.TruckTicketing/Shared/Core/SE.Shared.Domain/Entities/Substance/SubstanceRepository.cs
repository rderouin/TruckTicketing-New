using System.Linq;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.Substance;

public class SubstanceRepository : CosmosEFCoreSearchRepositoryBase<SubstanceEntity>
{
    public SubstanceRepository(ISearchResultsBuilder resultsBuilder, ISearchQueryBuilder queryBuilder, IAbstractContextFactory abstractContextFactory, IQueryableHelper queryableHelper) :
        base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }

    protected override IQueryable<T> ApplyKeywordSearch<T>(IQueryable<T> source, string keywords)
    {
        if (string.IsNullOrEmpty(keywords))
        {
            return source;
        }

        var keyword = keywords.ToLower();

        var typedSource = (IQueryable<SubstanceEntity>)source;

        return (IQueryable<T>)typedSource
           .Where(x => x.SubstanceName != null && x.SubstanceName.ToLower().Contains(keyword));
    }
}
