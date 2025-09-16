using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.FacilityService;

public class FacilityServiceSubstanceIndexRepository : CosmosEFCoreSearchRepositoryBase<FacilityServiceSubstanceIndexEntity>
{
    public const string Separator = "?";
    public FacilityServiceSubstanceIndexRepository(ISearchResultsBuilder resultsBuilder, ISearchQueryBuilder queryBuilder, IAbstractContextFactory abstractContextFactory, IQueryableHelper queryableHelper) :
        base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
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
        if (source is not IQueryable<FacilityServiceSubstanceIndexEntity> typedSource)
        {
            return source;
        }

        // apply filters
        if (keywords.HasText())
        {
            var value = keywords!.ToLower().Split("?").Select(term => term.Trim()).ToArray();
            var serviceTypeTerm = value.FirstOrDefault();
            var substanceWasteCodeTerm = value.LastOrDefault();

            if (serviceTypeTerm.HasText())
            {   
                typedSource = typedSource.Where(e =>
                                                     e.ServiceTypeName.ToLower().Contains(serviceTypeTerm) ||
                                                     e.FacilityServiceNumber.ToLower().Contains(serviceTypeTerm));
            }

            if (substanceWasteCodeTerm.HasText() && serviceTypeTerm != substanceWasteCodeTerm)
            {
                typedSource = typedSource.Where(e => e.Substance.ToLower().Contains(substanceWasteCodeTerm) ||
                                                     e.WasteCode.ToLower().Contains(substanceWasteCodeTerm));
            }
        }

        return (IQueryable<T>)typedSource;
    }
}
