using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.SalesLine;

public class SalesLineRepository : CosmosEFCoreSearchRepositoryBase<SalesLineEntity>
{
    public SalesLineRepository(ISearchResultsBuilder resultsBuilder,
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

    protected override IQueryable<T> ApplyFilter<T>(IQueryable<T> source, SearchCriteria criteria, IContext context)
    {
        var isIncludeVoidLines = false;
        if (source is not IQueryable<SalesLineEntity> typedSource)
        {
            return base.ApplyFilter(source, criteria, context);
        }

        var query = base.ApplyFilter(typedSource, criteria, context);

        //Exclude displaying Void SalesLines on grid only when DropDown Status filter doesn't pass Void filter
        if (criteria.Filters.ContainsKey(nameof(SalesLineEntity.Status)))
        {
            var filterValueType = criteria.Filters[nameof(SalesLineEntity.Status)]?.GetType();
            if (filterValueType == typeof(AxiomFilter))
            {
                var af = (AxiomFilter)criteria.Filters[nameof(SalesLineEntity.Status)];
                if (af.Axioms.Any() && af.Axioms.Any(x => x.Operator.Equals(CompareOperators.eq) && x.Value.Equals(SalesLineStatus.Void.ToString())))
                {
                    isIncludeVoidLines = true;
                }
            }
        }

        if (!isIncludeVoidLines)
        {
            query = query.Where(x => x.Status != SalesLineStatus.Void);
        }

        return (IQueryable<T>)query;
    }

    public IQueryable<T> ApplyKeywordSearchImpl<T>(IQueryable<T> source, string keywords)
    {
        if (source is not IQueryable<SalesLineEntity> typedSource)
        {
            return source;
        }

        if (keywords.HasText())
        {
            var value = keywords!.ToLower();

            typedSource = typedSource.Where(x => (x.SalesLineNumber != null && x.SalesLineNumber.ToLower().Contains(value)) ||
                                                 (x.TruckTicketNumber != null && x.TruckTicketNumber.ToLower().Contains(value)) ||
                                                 (x.CustomerName != null && x.CustomerName.ToLower().Contains(value)) ||
                                                 (x.GeneratorName != null && x.GeneratorName.ToLower().Contains(value)) ||
                                                 (x.LoadConfirmationNumber != null && x.LoadConfirmationNumber.ToLower().Contains(value)) ||
                                                 (x.ProformaInvoiceNumber != null && x.ProformaInvoiceNumber.ToLower().Contains(value)) ||
                                                 (x.SourceLocationIdentifier != null && x.SourceLocationIdentifier.ToLower().Contains(value)) ||
                                                 (x.ManifestNumber != null && x.ManifestNumber.ToLower().Contains(value)) ||
                                                 (x.BillOfLading != null && x.BillOfLading.ToLower().Contains(value)) ||
                                                 (x.EDIFieldsValueString != null && x.EDIFieldsValueString.ToLower().Contains(value)));
        }

        return (IQueryable<T>)typedSource;
    }
}
