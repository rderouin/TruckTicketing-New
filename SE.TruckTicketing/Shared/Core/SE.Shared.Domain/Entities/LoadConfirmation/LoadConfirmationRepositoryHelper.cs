using System;
using System.Linq;
using System.Linq.Expressions;

using SE.TruckTicketing.Contracts.Lookups;

using Trident;
using Trident.Extensions;
using Trident.Search;

using static System.Int32;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public class LoadConfirmationRepositoryHelper
{
    private const string OnDemandFilterKey = "OnDemand";
    public const string IgnoreFiltersKey = "IgnoreFilters";
    public const string NoSalesLinesKey = "NoSalesLines";
    public const string FieldTicketsKey = "FieldTickets";
    public const string SelectedYearKey = "SelectedYear";
    public const string SelectedMonthsKey = "SelectedMonths";

    public static object GetIsOnDemandFilterAndRemovedUntypedFilter(SearchCriteria criteria)
    {
        return GetValueFromFilter(criteria, OnDemandFilterKey);
    }

    public static object GetIgnoreFiltersValue(SearchCriteria criteria)
    {
        return GetValueFromFilter(criteria, IgnoreFiltersKey);
    }

    public static object GetShowNoSalesLineFilter(SearchCriteria criteria)
    {
        return GetValueFromFilter(criteria, NoSalesLinesKey);
    }

    public static object GetShowFieldTicketsFilter(SearchCriteria criteria)
    {
        return GetValueFromFilter(criteria, FieldTicketsKey);
    }

    public static object GetSelectedYearFilter(SearchCriteria criteria)
    {
        return GetValueFromFilter(criteria, SelectedYearKey);
    }

    public static object GetSelectedMonthsFilter(SearchCriteria criteria)
    {
        return GetValueFromFilter(criteria, SelectedMonthsKey);
    }

    private static object GetValueFromFilter(SearchCriteria criteria, string key)
    {
        object axiomFilter = null;
        criteria.Filters?.Remove(key, out axiomFilter);
        return axiomFilter;
    }

    public static IQueryable<LoadConfirmationEntity> ShowNoSalesLinesOnlyIfApplicable(IQueryable<LoadConfirmationEntity> query, object showNoSalesLines)
    {
        bool.TryParse(showNoSalesLines?.ToString(), out var result);

        if (result)
        {
            query = query.Where(lc => lc.SalesLineCount == 0);
        }

        return query;
    }

    public static IQueryable<LoadConfirmationEntity> ShowFieldTicketsOnlyIfApplicable(IQueryable<LoadConfirmationEntity> query, object showFieldTickets)
    {
        bool.TryParse(showFieldTickets?.ToString(), out var result);

        if (result)
        {
            query = query.Where(lc => lc.FieldTicketsUploadEnabled == true);
        }

        return query;
    }

    public static IQueryable<LoadConfirmationEntity> ApplyOnDemandDatesFilter(IQueryable<LoadConfirmationEntity> query, DateTimeOffset currentAlbertaOffsetDate)
    {
        query = query.Where(lc => (
                                   lc.EndDate != null && lc.EndDate < currentAlbertaOffsetDate) ||
                                   (lc.Frequency == LoadConfirmationFrequency.OnDemand.ToString() && lc.StartDate < currentAlbertaOffsetDate));

        return query;
    }

    public static IQueryable<LoadConfirmationEntity> ApplyOpenStatusFilter(IQueryable<LoadConfirmationEntity> query)
    {
        return query.Where(lc => lc.Status == LoadConfirmationStatus.Open);
    }

    public static IQueryable<LoadConfirmationEntity> ExcludeVoidStatusIfApplicable(SearchCriteria criteria, IQueryable<LoadConfirmationEntity> query)
    {
        return ExcludeStatusIfApplicable(criteria, query, LoadConfirmationStatus.Void);
    }

    public static IQueryable<LoadConfirmationEntity> ExcludePostedStatusIfApplicable(SearchCriteria criteria, IQueryable<LoadConfirmationEntity> query)
    {
        return ExcludeStatusIfApplicable(criteria, query, LoadConfirmationStatus.Posted);
    }

    private static IQueryable<LoadConfirmationEntity> ExcludeStatusIfApplicable(SearchCriteria criteria, IQueryable<LoadConfirmationEntity> query, LoadConfirmationStatus status)
    {
        object axiomFilter = null;

        var hasFilter = criteria.Filters?.TryGetValue(nameof(LoadConfirmationEntity.Status), out axiomFilter) ?? false;

        var shouldIgnoreFilter = hasFilter && axiomFilter.ToJson().Contains(status.ToString());

        if (!shouldIgnoreFilter)
        {
            query = query.Where(lc => lc.Status != status);
        }

        return query;
    }

    public static IQueryable<LoadConfirmationEntity> ExcludeNoneFrequencyIfApplicable(SearchCriteria criteria, IQueryable<LoadConfirmationEntity> query)
    {
        return ExcludeFrequencyIfApplicable(criteria, query, LoadConfirmationFrequency.None);
    }

    private static IQueryable<LoadConfirmationEntity> ExcludeFrequencyIfApplicable(SearchCriteria criteria, IQueryable<LoadConfirmationEntity> query, LoadConfirmationFrequency freq)
    {
        object axiomFilter = null;

        var hasFilter = criteria.Filters?.TryGetValue(nameof(LoadConfirmationEntity.Frequency), out axiomFilter) ?? false;

        var shouldIgnoreFilter = hasFilter && axiomFilter.ToJson().Contains(freq.ToString());

        if (!shouldIgnoreFilter)
        {
            query = query.Where(lc => lc.Frequency != freq.ToString());
        }

        return query;
    }

    public static IQueryable<LoadConfirmationEntity> IncludeOnDemandFilterIfApplicable(IQueryable<LoadConfirmationEntity> query, object predicate, DateTimeOffset currentAlbertaOffsetDate)
    {
        bool.TryParse(predicate?.ToString(), out var axiomFilter);

        if (axiomFilter)
        {
            query = ApplyOpenStatusFilter(query);

            query = ApplyOnDemandDatesFilter(query, currentAlbertaOffsetDate);
        }

        return query;
    }

    public static IQueryable<LoadConfirmationEntity> FilterTicketDatesBySelectedYear(IQueryable<LoadConfirmationEntity> query, object predicate)
    {
        TryParse(predicate?.ToString(), out int selectedYear);

        if (selectedYear != 0)
        {
            var january01OfSelectedYear = new DateTimeOffset(selectedYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var december31OfSelectedYear = new DateTimeOffset(selectedYear, 12, 31, 23, 59, 59, TimeSpan.Zero);

            return query.Where(lc => lc.TicketStartDate >= january01OfSelectedYear && lc.TicketStartDate <= december31OfSelectedYear);
        }
        else
        {
            selectedYear = DateTime.UtcNow.Year;
            var january01OfSelectedYear = new DateTimeOffset(selectedYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var december31OfSelectedYear = new DateTimeOffset(selectedYear, 12, 31, 23, 59, 59, TimeSpan.Zero);

            return query.Where(lc => lc.TicketStartDate >= january01OfSelectedYear && lc.TicketStartDate <= december31OfSelectedYear);
        }

    }

    public static IQueryable<LoadConfirmationEntity> FilterTicketDatesBySelectedMonthsAndYear(IQueryable<LoadConfirmationEntity> query, object selectedYearFilter, object selectedMonthsFilter)
    {
        if (selectedYearFilter == null && selectedMonthsFilter == null)
        {
            return query;
        }

        if (selectedYearFilter != null && selectedMonthsFilter == null)
        {
            return FilterTicketDatesBySelectedYear(query, selectedYearFilter);
        }

        TryParse(selectedYearFilter?.ToString(), out var yearFilter);

        return BuildMonthsQuery(query, selectedMonthsFilter, yearFilter);

    }

    private static IQueryable<LoadConfirmationEntity> BuildMonthsQuery(IQueryable<LoadConfirmationEntity> query, object selectedMonthsFilter, int year)
    {
        if (selectedMonthsFilter.GetType() == typeof(AxiomFilter))
        {
            var monthsFilter = (AxiomFilter)selectedMonthsFilter;

            if (monthsFilter.Axioms.Any())

            {
                if (monthsFilter.Axioms.Count == 1)
                {
                    return BuildOneMonthQuery(query, year, monthsFilter.Axioms[0]);
                }

                var month = monthsFilter.GetIntFromFilter(0);

                var firstExpression = SameTicketMonthAndYear(month, year);

                for (int i = 1; i < monthsFilter.Axioms.Count; i++)
                {
                    month = monthsFilter.GetIntFromFilter(i);

                    var subsequentExpression = SameTicketMonthAndYear(month, year);

                    firstExpression = firstExpression.OrElse(subsequentExpression);
                }

                query = query.Where(firstExpression);

            }
        }

        return query;
    }

    private static IQueryable<LoadConfirmationEntity> BuildOneMonthQuery(IQueryable<LoadConfirmationEntity> query, int year, Axiom axiom)
    {
        query = query.Where(SameTicketMonthAndYear(axiom.GetIntFromFilter(), year));
        return query;
    }

    private static Expression<Func<LoadConfirmationEntity, bool>> SameTicketMonthAndYear(int month, int year)
    {
        var selectedStartOfMonth = month.GetFirstDayOfMonth(year);
        var selectedEndOfMonth = month.GetLastDayOfMonth(year);

        return lc => lc.TicketStartDate >= selectedStartOfMonth && lc.TicketEndDate <= selectedEndOfMonth;
    }




}
