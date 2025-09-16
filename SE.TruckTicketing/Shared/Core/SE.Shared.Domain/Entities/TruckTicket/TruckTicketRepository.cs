using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.TruckTicket;

public class TruckTicketRepository : CosmosEFCoreSearchRepositoryBase<TruckTicketEntity>
{
    public TruckTicketRepository(ISearchResultsBuilder resultsBuilder,
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
        if (source is not IQueryable<TruckTicketEntity> typedSource)
        {
            return base.ApplyFilter(source, criteria, context);
        }

        criteria.Filters.Remove("Aged", out var agedTicketCutOffDaysFilter);
        criteria.Filters.Remove("SalesLineIds", out var includeSalesLinesFilter);
        criteria.Filters.Remove(nameof(TruckTicketEntity.BillOfLading).AsCaseInsensitiveFilterKey(), out var billOfLadingFilterValue);

        var query = base.ApplyFilter(typedSource, criteria, context);

        if (int.TryParse(agedTicketCutOffDaysFilter?.ToString(), out var days))
        {
            var cutOffDate = DateTime.Today.AddDays(-days);
            query = query.Where(ticket => !(ticket.Status == TruckTicketStatus.Void || (ticket.Status == TruckTicketStatus.Invoiced &&
                                                                                        ticket.LoadDate <= cutOffDate)));
        }

        if (bool.TryParse(includeSalesLinesFilter?.ToString(), out var includeTicketsWithSalesLines) && includeTicketsWithSalesLines)
        {
            query = query.Where(ticket => ticket.SalesLineIds.Raw != null && ticket.SalesLineIds.Raw.Trim() != "");
        }

        if (billOfLadingFilterValue is string billOfLading && billOfLading.HasText())
        {
            var normalizedBillOfLading = billOfLading.ToLower();
            query = query.Where(ticket => ticket.BillOfLading.ToLower() == normalizedBillOfLading);
        }

        return (IQueryable<T>)query;
    }

    public IQueryable<T> ApplyKeywordSearchImpl<T>(IQueryable<T> source, string keywords)
    {
        if (source is not IQueryable<TruckTicketEntity> typedSource)
        {
            return source;
        }

        if (keywords.HasText())
        {
            typedSource = typedSource.Where(x => (x.CustomerName != null && x.CustomerName.ToLower().Contains(keywords.ToLower())) ||
                                                 (x.SourceLocationFormatted != null && x.SourceLocationFormatted.ToLower().Contains(keywords.ToLower())) ||
                                                 (x.SourceLocationCode != null && x.SourceLocationCode.ToLower().Contains(keywords.ToLower())) ||
                                                 (x.SourceLocationName != null && x.SourceLocationName.ToLower().Contains(keywords.ToLower())) ||
                                                 (x.SourceLocationUnformatted != null && x.SourceLocationUnformatted.ToLower().Contains(keywords.ToLower())) ||
                                                 (x.BillingCustomerName != null && x.BillingCustomerName.ToLower().Contains(keywords.ToLower())) ||
                                                 (x.TicketNumber != null && x.TicketNumber.ToLower().Contains(keywords.ToLower())) ||
                                                 (x.TruckingCompanyName != null && x.TruckingCompanyName.ToLower().Contains(keywords.ToLower())) ||
                                                 (x.BillOfLading != null && x.BillOfLading.ToLower().Contains(keywords.ToLower())) ||
                                                 (x.SubstanceName != null && x.SubstanceName.ToLower().Contains(keywords.ToLower())) ||
                                                 (x.ManifestNumber != null && x.ManifestNumber.ToLower().Contains(keywords.ToLower())));
        }

        return (IQueryable<T>)typedSource;
    }
}
