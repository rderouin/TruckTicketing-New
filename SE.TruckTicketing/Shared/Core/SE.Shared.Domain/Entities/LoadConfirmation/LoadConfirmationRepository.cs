using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public class LoadConfirmationRepository : CosmosEFCoreSearchRepositoryBase<LoadConfirmationEntity>
{
    public LoadConfirmationRepository(ISearchResultsBuilder resultsBuilder,
                                      ISearchQueryBuilder queryBuilder,
                                      IAbstractContextFactory abstractContextFactory,
                                      IQueryableHelper queryableHelper)
        : base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }

    protected override IQueryable<T> ApplyFilter<T>(IQueryable<T> source, SearchCriteria criteria, IContext context)
    {
        if (source is not IQueryable<LoadConfirmationEntity> stronglyTypedSource)
        {
            return base.ApplyFilter(source, criteria, context);
        }

        var isOnDemandFilterValue = LoadConfirmationRepositoryHelper.GetIsOnDemandFilterAndRemovedUntypedFilter(criteria);
        var doIgnoreFilterValue = LoadConfirmationRepositoryHelper.GetIgnoreFiltersValue(criteria);
        var showNoSalesLinesValue = LoadConfirmationRepositoryHelper.GetShowNoSalesLineFilter(criteria);
        var showFieldTicketsValue = LoadConfirmationRepositoryHelper.GetShowFieldTicketsFilter(criteria);
        var selectedYearFilter = LoadConfirmationRepositoryHelper.GetSelectedYearFilter(criteria);
        var selectedMonthsFilter = LoadConfirmationRepositoryHelper.GetSelectedMonthsFilter(criteria);

        if (doIgnoreFilterValue != null)
        {
            criteria.Filters?.Remove("InvoiceStatus");
            criteria.Filters?.Remove("Status");
            criteria.Filters?.Remove("Frequency");

            return (IQueryable<T>)base.ApplyFilter(stronglyTypedSource, criteria, context);
        }

        var query = base.ApplyFilter(stronglyTypedSource, criteria, context);

        return BuildCustomQuery<T>(criteria, query, isOnDemandFilterValue, showNoSalesLinesValue, showFieldTicketsValue, selectedYearFilter, selectedMonthsFilter);
    }

    public static IQueryable<T> BuildCustomQuery<T>(SearchCriteria criteria,
                                                    IQueryable<LoadConfirmationEntity> query,
                                                    object isOnDemandFilterValue,
                                                    object showNoSalesLinesValue,
                                                    object showFieldFicketsValue,
                                                    object selectedYearFilter,
                                                    object selectedMonthsFilter)
    {
        query = LoadConfirmationRepositoryHelper.ExcludeVoidStatusIfApplicable(criteria, query);
        query = LoadConfirmationRepositoryHelper.ExcludePostedStatusIfApplicable(criteria, query);
        query = LoadConfirmationRepositoryHelper.ExcludeNoneFrequencyIfApplicable(criteria, query);
        query = LoadConfirmationRepositoryHelper.IncludeOnDemandFilterIfApplicable(query, isOnDemandFilterValue, DateTimeOffset.UtcNow.ToAlbertaOffset());
        query = LoadConfirmationRepositoryHelper.ShowNoSalesLinesOnlyIfApplicable(query, showNoSalesLinesValue);
        query = LoadConfirmationRepositoryHelper.ShowFieldTicketsOnlyIfApplicable(query, showFieldFicketsValue);
        query = LoadConfirmationRepositoryHelper.FilterTicketDatesBySelectedMonthsAndYear(query, selectedYearFilter, selectedMonthsFilter);

        return (IQueryable<T>)query;
    }

    [ExcludeFromCodeCoverage(Justification = "The implementation is offloaded into a separate method.")]
    protected override IQueryable<T> ApplyKeywordSearch<T>(IQueryable<T> source, string keywords)
    {
        return ApplyKeywordSearchImpl(source, keywords);
    }

    public IQueryable<T> ApplyKeywordSearchImpl<T>(IQueryable<T> source, string keywords)
    {
        if (source is not IQueryable<LoadConfirmationEntity> typedSource)
        {
            return source;
        }

        if (!keywords.HasText())
        {
            return (IQueryable<T>)typedSource;
        }

        var lowerKeywords = keywords!.ToLower();

        typedSource = typedSource.Where(x =>
                                            (x.Number != null && x.Number.ToLower().Contains(lowerKeywords)) ||
                                            (x.SignatoryNames != null && x.SignatoryNames.ToLower().Contains(lowerKeywords)) ||
                                            (x.InvoiceNumber != null && x.InvoiceNumber.ToLower().Contains(lowerKeywords)) ||
                                            (x.GlInvoiceNumber != null && x.GlInvoiceNumber.ToLower().Contains(lowerKeywords)) ||
                                            (x.BillingConfigurationName != null && x.BillingConfigurationName.ToLower().Contains(lowerKeywords)));

        return (IQueryable<T>)typedSource;
    }
}
