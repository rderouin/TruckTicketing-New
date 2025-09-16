using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.LegalEntity;

public class LegalRepository : CosmosEFCoreSearchRepositoryBase<LegalEntityEntity>
{
    public const string PermissionType = nameof(PermissionType);

    public LegalRepository(ISearchResultsBuilder resultsBuilder,
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
        if (source is not IQueryable<LegalEntityEntity> typedSource)
        {
            return source;
        }

        if (keywords.HasText())
        {
            typedSource = typedSource.Where(x => x.Name.ToLower().Contains(keywords.ToLower()) || x.Code.ToLower().Contains(keywords.ToLower()));
        }

        return (IQueryable<T>)typedSource;
    }
}
