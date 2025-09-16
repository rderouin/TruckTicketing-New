using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.MaterialApproval;

public class MaterialApprovalRepository : CosmosEFCoreSearchRepositoryBase<MaterialApprovalEntity>
{
    public MaterialApprovalRepository(ISearchResultsBuilder resultsBuilder,
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
        // ensure a proper type
        if (source is not IQueryable<MaterialApprovalEntity> typedSource)
        {
            return source;
        }

        // apply filters
        if (keywords.HasText())
        {
            var value = keywords!.ToLower();
            typedSource = typedSource.Where(x => (x.GeneratorName != null && x.GeneratorName.ToLower().Contains(value)) ||
                                                 (x.Description != null && x.Description.ToLower().Contains(value))
                                              || (x.SourceLocation != null && x.SourceLocation.ToLower().Contains(value))
                                              || (x.SourceLocationFormattedIdentifier != null && x.SourceLocationFormattedIdentifier.ToLower().Contains(value)) ||
                                                 (x.SourceLocationUnformattedIdentifier != null && x.SourceLocationUnformattedIdentifier.ToLower().Contains(value.Replace("-", ""))) ||
                                                 (x.FacilityServiceName != null && x.FacilityServiceName.ToLower().Contains(value))
                                              || (x.ThirdPartyAnalyticalCompanyName != null && x.ThirdPartyAnalyticalCompanyName.ToLower().Contains(value)) ||
                                                 (x.BillingCustomerName != null && x.BillingCustomerName.ToLower().Contains(value))
                                              || (x.TruckingCompanyName != null && x.TruckingCompanyName.ToLower().Contains(value)) ||
                                                 (x.WasteCodeName != null && x.WasteCodeName.ToLower().Contains(value))
                                              || (x.AdditionalServiceName != null && x.AdditionalServiceName.ToLower().Contains(value))
                                              || (x.MaterialApprovalNumber != null && x.MaterialApprovalNumber.ToLower().Contains(value)));
        }

        return (IQueryable<T>)typedSource;
    }
}
