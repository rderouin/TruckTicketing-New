using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.Role;

public class RoleRepository : CosmosEFCoreSearchRepositoryBase<RoleEntity>
{
    public const string PermissionType = nameof(PermissionType);

    public RoleRepository(ISearchResultsBuilder resultsBuilder,
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
        if (source is not IQueryable<RoleEntity> typedSource)
        {
            return source;
        }

        if (keywords.HasText())
        {
            typedSource = typedSource.Where(x => x.Name.ToLower().Contains(keywords.ToLower()));
        }

        return (IQueryable<T>)typedSource;
    }

    protected override IQueryable<T> ApplyFilter<T>(IQueryable<T> source, SearchCriteria criteria, IContext context)
    {
        if (source is not IQueryable<RoleEntity> typedSource)
        {
            return base.ApplyFilter(source, criteria, context);
        }

        if (!criteria.Filters.ContainsKey(PermissionType))
        {
            return base.ApplyFilter(source, criteria, context);
        }

        criteria.Filters.Remove(PermissionType, out var listOfPermissionsFilter);
        var query = base.ApplyFilter(typedSource, criteria, context);
        return (IQueryable<T>)query;
    }
}
