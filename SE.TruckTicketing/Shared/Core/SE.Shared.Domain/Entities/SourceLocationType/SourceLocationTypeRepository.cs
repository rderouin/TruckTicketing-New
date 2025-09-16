using System.Linq;
using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.SourceLocationType;

public class SourceLocationTypeRepository : CosmosEFCoreSearchRepositoryBase<SourceLocationTypeEntity>
{
    public SourceLocationTypeRepository(ISearchResultsBuilder resultsBuilder, ISearchQueryBuilder queryBuilder, IAbstractContextFactory abstractContextFactory, IQueryableHelper queryableHelper) :
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

        var typedSource = (IQueryable<SourceLocationTypeEntity>)source;

        return (IQueryable<T>)typedSource
           .Where(x => x.Name != null && x.Name.ToLower().Contains(keyword));
    }
}
