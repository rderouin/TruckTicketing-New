using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

public class SpartanProductParameterRepository : CosmosEFCoreSearchRepositoryBase<SpartanProductParameterEntity>
{
    public SpartanProductParameterRepository(ISearchResultsBuilder resultsBuilder,
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
        if (source is not IQueryable<SpartanProductParameterEntity> typedSource)
        {
            return source;
        }

        if (keywords.HasText())
        {
            typedSource = typedSource.Where(x => x.ProductName != null && x.ProductName.ToLower().Contains(keywords.ToLower()));
        }

        return (IQueryable<T>)typedSource;
    }
}
