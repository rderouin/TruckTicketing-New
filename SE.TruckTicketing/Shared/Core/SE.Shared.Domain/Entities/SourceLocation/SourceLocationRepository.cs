using System.Linq;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.SourceLocation;

public class SourceLocationRepository : CosmosEFCoreSearchRepositoryBase<SourceLocationEntity>
{
    public SourceLocationRepository(ISearchResultsBuilder resultsBuilder, ISearchQueryBuilder queryBuilder, IAbstractContextFactory abstractContextFactory, IQueryableHelper queryableHelper) :
        base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }

    protected override IQueryable<T> ApplyKeywordSearch<T>(IQueryable<T> source, string keywords)
    {
        if (string.IsNullOrEmpty(keywords))
        {
            return source;
        }

        var typedSource = (IQueryable<SourceLocationEntity>)source;
        var keyword = keywords.ToLower();

        return (IQueryable<T>)typedSource
           .Where(x => (x.ProvinceOrStateString != null && x.ProvinceOrStateString.ToLower().Contains(keyword))
                    || (x.FieldName != null && x.FieldName.ToLower().Contains(keyword))
                    || (x.GeneratorAccountNumber != null && x.GeneratorAccountNumber.ToLower().Contains(keyword))
                    || (x.GeneratorName != null && x.GeneratorName.ToLower().Contains(keyword))
                    || (x.ContractOperatorName != null && x.ContractOperatorName.ToLower().Contains(keyword))
                    || (x.SourceLocationTypeName != null && x.SourceLocationTypeName.ToLower().Contains(keyword))
                    || (x.Identifier != null && x.Identifier.ToLower().Contains(keyword.ToLower().Replace("-", "")))
                    || (x.FormattedIdentifier != null && x.FormattedIdentifier.ToLower().Contains(keyword))
                    || (x.SourceLocationName != null && x.SourceLocationName.ToLower().Contains(keyword))
                    || (x.SourceLocationCode != null && x.SourceLocationCode.ToLower().Contains(keyword))
                    || (x.AssociatedSourceLocationCode != null && x.AssociatedSourceLocationCode.ToLower().Contains(keyword))
                    || (x.AssociatedSourceLocationIdentifier != null && x.AssociatedSourceLocationIdentifier.ToLower().Contains(keyword)));
    }
}
