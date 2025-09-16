using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Extensions;
using Trident.Search;

namespace SE.Shared.Domain.Entities.Invoices;

public class InvoiceRepository : CosmosEFCoreSearchRepositoryBase<InvoiceEntity>
{
    public InvoiceRepository(ISearchResultsBuilder resultsBuilder,
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
        if (source is not IQueryable<InvoiceEntity> typedSource)
        {
            return base.ApplyFilter(source, criteria, context);
        }

        object predicate = null;
        object showOnlyNoSalesLines = null;
        var hasStatusFilter = criteria.Filters?.TryGetValue(nameof(InvoiceEntity.Status), out predicate) ?? false;
        var ignoreVoidFilter = hasStatusFilter && predicate.ToJson().Contains(nameof(InvoiceStatus.Void));
        criteria.Filters?.Remove("NoSalesLines", out showOnlyNoSalesLines);

        var query = base.ApplyFilter(typedSource, criteria, context);

        // include voided invoices when SearchCriteria includes Void filter or when doing a keyword search       
        if (!ignoreVoidFilter && !criteria.Keywords.HasText())
        {
            query = query.Where(invoice => invoice.Status != InvoiceStatus.Void);
        }

        bool.TryParse(showOnlyNoSalesLines?.ToString(), out var includeOnlyNoSalesLines);
        if (includeOnlyNoSalesLines)
        {
            query = query.Where(invoice => invoice.SalesLineCount == 0);
        }

        return (IQueryable<T>)query;
    }

    private IQueryable<T> ApplyKeywordSearchImpl<T>(IQueryable<T> source, string keywords)
    {
        if (source is not IQueryable<InvoiceEntity> typedSource)
        {
            return source;
        }

        if (keywords.HasText())
        {
            var value = keywords!.ToLower();
            typedSource = typedSource.Where(x => (x.CustomerName != null && x.CustomerName.ToLower().Contains(value)) ||
                                                 (x.GlInvoiceNumber != null && x.GlInvoiceNumber.ToLower().Contains(value)) ||
                                                 (x.ProformaInvoiceNumber != null && x.ProformaInvoiceNumber.ToLower().Contains(value)) ||
                                                 (x.OriginalProformaInvoiceNumber != null && x.OriginalProformaInvoiceNumber.ToLower().Contains(value)) ||
                                                 (x.OriginalGlInvoiceNumber != null && x.OriginalGlInvoiceNumber.ToLower().Contains(value)) ||
                                                 (x.GeneratorNames != null && x.GeneratorNames.ToLower().Contains(value)) ||
                                                 (x.CustomerNumber != null && x.CustomerNumber.ToLower().Contains(value)) ||
                                                 (x.ManifestNumber != null && x.ManifestNumber.ToLower().Contains(value)) ||
                                                 (x.BillofLading != null && x.BillofLading.ToLower().Contains(value)) ||
                                                 (x.BillingConfigurationNames != null && x.BillingConfigurationNames.ToLower().Contains(value)) ||
                                                 (x.EDIFieldsValueString != null && x.EDIFieldsValueString.ToLower().Contains(value)) ||
                                                 (x.SourceLocationIdentifier != null && x.SourceLocationIdentifier.ToLower().Contains(value)) ||
                                                 (x.BillofLading != null && x.BillofLading.ToLower().Contains(value)));
        }

        return (IQueryable<T>)typedSource;
    }
}
