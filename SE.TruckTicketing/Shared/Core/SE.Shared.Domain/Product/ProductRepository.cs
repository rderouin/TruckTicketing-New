using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Product;

public class ProductRepository : CosmosEFCoreSearchRepositoryBase<ProductEntity>
{
    public ProductRepository(ISearchResultsBuilder resultsBuilder,
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

    private IQueryable<T> ApplyKeywordSearchImpl<T>(IQueryable<T> source, string keywords)
    {
        // ensure a proper type
        if (source is not IQueryable<ProductEntity> typedSource)
        {
            return source;
        }

        // apply filters
        if (keywords.HasText())
        {
            var value = keywords!.ToLower();
            typedSource = typedSource.Where(x => (x.Name != null && x.Name.ToLower().Contains(value)) || (x.Number != null && x.Number.Contains(value)));
        }

        return (IQueryable<T>)typedSource;
    }

    protected override IQueryable<T> ApplyFilter<T>(IQueryable<T> source, SearchCriteria criteria, IContext context)
    {
        if (source is not IQueryable<ProductEntity> typedSource)
        {
            return base.ApplyFilter(source, criteria, context);
        }

        var query = base.ApplyFilter(typedSource, criteria, context);

        if (!criteria.Filters.ContainsKey(nameof(ProductEntity.IsActive).AsCaseInsensitiveFilterKey()))
        {
            query = query.Where(product => product.IsActive);
        }

        return (IQueryable<T>)query;
    }
}
