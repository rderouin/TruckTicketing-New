using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.VolumeChange;

public class VolumeChangeRepository : CosmosEFCoreSearchRepositoryBase<VolumeChangeEntity>
{
    public VolumeChangeRepository(ISearchResultsBuilder resultsBuilder,
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
        if (source is not IQueryable<VolumeChangeEntity> typedSource)
        {
            return source;
        }

        if (keywords.HasText())
        {
            var value = keywords!.ToLower();

            if (Enum.TryParse(value, true, out Stream stream))
            {
                typedSource = typedSource.Where(x => (x.FacilityName != null && x.FacilityName.ToLower().Contains(value)) ||
                                                     x.ProcessOriginal == stream);
            }
            else
            {
                typedSource = typedSource.Where(x => x.FacilityName != null && x.FacilityName.ToLower().Contains(value));
            }
        }

        return (IQueryable<T>)typedSource;
    }
}
