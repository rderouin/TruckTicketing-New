using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.Sequences;

public class SequenceRepository : CosmosEFCoreSearchRepositoryBase<SequenceEntity>
{
    public SequenceRepository(ISearchResultsBuilder resultsBuilder,
                              ISearchQueryBuilder queryBuilder,
                              IAbstractContextFactory abstractContextFactory,
                              IQueryableHelper queryableHelper)
        : base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }
}
