using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.UserProfile;

public class UserProfileRepository : CosmosEFCoreSearchRepositoryBase<UserProfileEntity>
{
    public UserProfileRepository(ISearchResultsBuilder resultsBuilder,
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
        if (source is not IQueryable<UserProfileEntity> typedSource)
        {
            return source;
        }

        if (keywords.HasText())
        {
            typedSource = typedSource.Where(x => x.GivenName.ToLower().Contains(keywords.ToLower()) ||
                                                 x.DisplayName.ToLower().Contains(keywords.ToLower()) ||
                                                 x.Surname.ToLower().Contains(keywords.ToLower()));
        }

        return (IQueryable<T>)typedSource;
    }
}
