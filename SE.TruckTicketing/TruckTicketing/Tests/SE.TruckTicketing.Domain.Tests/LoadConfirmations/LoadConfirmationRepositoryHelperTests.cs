using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Search;

namespace SE.TruckTicketing.Domain.Tests.LoadConfirmations;

[TestClass]
public class LoadConfirmationRepositoryHelperTests
{
    [TestMethod]
    public void FilterTicketDatesBySelectedMonthsAndYear_DefaultYear_MonthFilterOneMonth()
    {
        //setup
        var thisMonthId = Guid.NewGuid();
        
        var utcNow = new DateTimeOffset(2023, 2, 2, 13, 15, 0, TimeSpan.Zero);
        var lastMonth = new TestMonth(utcNow.AddMonths(-1).Month, utcNow.Year);
        var thisMonth = new TestMonth(utcNow.Month, utcNow.Year);
        var nextMonth = new TestMonth(utcNow.AddMonths(1).Month, utcNow.Year);

        var list = new List<LoadConfirmationEntity>
        {
            new(){TicketStartDate = lastMonth.FirstOfMonth, TicketEndDate = lastMonth.LastOfMonth},
            new(){Id = thisMonthId, TicketStartDate = thisMonth.FirstOfMonth, TicketEndDate = thisMonth.LastOfMonth},
            new(){TicketStartDate = nextMonth.FirstOfMonth, TicketEndDate = nextMonth.LastOfMonth},
        };

        //imitate how the ui sends it to the repo.
        var userSelectedMonth = thisMonth.FirstOfMonth.AddDays(1);

        var monthsFilter = new AxiomFilter { Axioms = new()
            {
                new() { Key = LoadConfirmationRepositoryHelper.SelectedMonthsKey, Value = userSelectedMonth.Month,},
            }
        };
        
        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.FilterTicketDatesBySelectedMonthsAndYear(query, thisMonth.FirstOfMonth.Year, monthsFilter);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(1, resultingCollection.Count);
        Assert.AreEqual(thisMonthId, resultingCollection[0].Id);
    }

    [TestMethod]
    public void FilterTicketDatesBySelectedMonthsAndYear_YearSelected_MonthFilterTwoMonths_MultiYearData()
    {
        //setup
        var thisMonthId = Guid.NewGuid();
        var lastMonthId = Guid.NewGuid();

        var utcNow = new DateTimeOffset(2023, 2, 2, 13, 15, 0, TimeSpan.Zero);
        var lastMonth = new TestMonth(utcNow.AddMonths(-1).Month, utcNow.Year);
        var lastMonthNextYear = lastMonth.FirstOfMonth.AddYears(1);
        var thisMonth = new TestMonth(utcNow.Month, utcNow.Year);
        var nextMonth = new TestMonth(utcNow.AddMonths(1).Month, utcNow.Year);
        var nextMonthNextYear = nextMonth.FirstOfMonth.AddYears(1).AddMonths(1);
        
        var list = new List<LoadConfirmationEntity>
        {
            new(){Id = lastMonthId, TicketStartDate = lastMonth.FirstOfMonth, TicketEndDate = lastMonth.LastOfMonth},
            new(){Id = thisMonthId, TicketStartDate = thisMonth.FirstOfMonth, TicketEndDate = thisMonth.LastOfMonth},
            new(){TicketStartDate = nextMonth.FirstOfMonth, TicketEndDate = nextMonth.LastOfMonth},
            new(){TicketStartDate = lastMonthNextYear, TicketEndDate = lastMonthNextYear},
            new(){TicketStartDate = nextMonthNextYear, TicketEndDate = nextMonthNextYear},
        };

        //imitate how the ui sends it to the repo.
        var monthsFilter = new AxiomFilter
        {
            Axioms = new()
            {
                new() { Key = LoadConfirmationRepositoryHelper.SelectedMonthsKey + "1", Value = thisMonth.FirstOfMonth.Month,},
                new() { Key = LoadConfirmationRepositoryHelper.SelectedMonthsKey + "2", Value = lastMonth.FirstOfMonth.Month,},
            }
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.FilterTicketDatesBySelectedMonthsAndYear(query, thisMonth.FirstOfMonth.Year, monthsFilter);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(2, resultingCollection.Count);
        Assert.AreEqual(lastMonthId, resultingCollection[0].Id);
        Assert.AreEqual(thisMonthId, resultingCollection[1].Id);
    }

    [TestMethod]
    public void FilterTicketDatesBySelectedMonthsAndYear_YearSelected_MonthFilterTwoMonths()
    {
        //setup
        var thisMonthId = Guid.NewGuid();
        var lastMonthId = Guid.NewGuid();

        var utcNow = new DateTimeOffset(2023, 2, 2, 13, 15, 0, TimeSpan.Zero);
        var lastMonth = new TestMonth(utcNow.AddMonths(-1).Month, utcNow.Year);
        var thisMonth = new TestMonth(utcNow.Month, utcNow.Year);
        var nextMonth = new TestMonth(utcNow.AddMonths(1).Month, utcNow.Year);

        var list = new List<LoadConfirmationEntity>
        {
            new(){Id = lastMonthId, TicketStartDate = lastMonth.FirstOfMonth, TicketEndDate = lastMonth.LastOfMonth},
            new(){Id = thisMonthId, TicketStartDate = thisMonth.FirstOfMonth, TicketEndDate = thisMonth.LastOfMonth},
            new(){TicketStartDate = nextMonth.FirstOfMonth, TicketEndDate = nextMonth.LastOfMonth},
        };

        //imitate how the ui sends it to the repo.
        var monthsFilter = new AxiomFilter
        {
            Axioms = new()
            {
                new() { Key = LoadConfirmationRepositoryHelper.SelectedMonthsKey + "1", Value = thisMonth.FirstOfMonth.Month,},
                new() { Key = LoadConfirmationRepositoryHelper.SelectedMonthsKey + "2", Value = lastMonth.FirstOfMonth.Month,},
            }
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.FilterTicketDatesBySelectedMonthsAndYear(query, thisMonth.FirstOfMonth.Year, monthsFilter);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(2, resultingCollection.Count);
        Assert.AreEqual(lastMonthId, resultingCollection[0].Id);
        Assert.AreEqual(thisMonthId, resultingCollection[1].Id);
    }
    
    [TestMethod]
    public void FilterTicketDatesBySelectedMonthsAndYear_OnlyYearSelected_MonthFilterNull()
    {
        //setup
        var thisYearId = Guid.NewGuid();
        
        var lastYear = DateTime.Today.AddYears(-1);
        var thisYear = DateTime.Today;
        var nextYear = DateTime.Today.AddYears(1);

        var list = new List<LoadConfirmationEntity>
        {
            new(){TicketStartDate = lastYear, TicketEndDate = lastYear.AddDays(1)},
            new(){Id=thisYearId, TicketStartDate = thisYear, TicketEndDate = thisYear.AddDays(1)},
            new(){TicketStartDate = nextYear, TicketEndDate = nextYear.AddDays(1)},
        };

        var query = list.AsQueryable();
        
        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.FilterTicketDatesBySelectedMonthsAndYear(query, thisYear.Year, null);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(1, resultingCollection.Count);
        Assert.AreEqual(thisYearId, resultingCollection[0].Id);
    }

    [TestMethod]
    public void FilterTicketDatesBySelectedMonthsAndYear_YearFilterNull_MonthFilterNull()
    {
        //setup
        var thisYearId = Guid.NewGuid();
        var lastYearId = Guid.NewGuid();
        var nextYearId = Guid.NewGuid();
        var lastYear = DateTime.Today.AddYears(-1);
        var thisYear = DateTime.Today;
        var nextYear = DateTime.Today.AddYears(1);

        var list = new List<LoadConfirmationEntity>
        {
            new(){Id = lastYearId, TicketStartDate = lastYear, TicketEndDate = lastYear.AddDays(1)},
            new(){Id= thisYearId, TicketStartDate = thisYear, TicketEndDate = thisYear.AddDays(1)},
            new(){Id = nextYearId, TicketStartDate = nextYear, TicketEndDate = nextYear.AddDays(1)},
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.FilterTicketDatesBySelectedMonthsAndYear(query, null, null);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(3, resultingCollection.Count);
        Assert.AreEqual(lastYearId, resultingCollection[0].Id);
        Assert.AreEqual(thisYearId, resultingCollection[1].Id);
        Assert.AreEqual(nextYearId, resultingCollection[2].Id);
    }

    [TestMethod]
    public void GetSelectedYearFilter_FindsOne()
    {
        //setup
        SearchCriteria criteria = new SearchCriteria();
        criteria.Filters.Add(LoadConfirmationRepositoryHelper.SelectedYearKey, 2024);

        //execute
        var result = LoadConfirmationRepositoryHelper.GetSelectedYearFilter(criteria);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2024, result);
    }

    [TestMethod]
    public void FilterTicketDatesBySelectedYear_FindsOneResultForThisYear()
    {
        //setup
        var thisYearId = Guid.NewGuid();
        var lastYear = DateTime.Today.AddYears(-1);
        var thisYear = DateTime.Today;
        var nextYear = DateTime.Today.AddYears(1);

        var list = new List<LoadConfirmationEntity>
        {
            new(){TicketStartDate = lastYear, TicketEndDate = lastYear.AddDays(1)},
            new(){Id=thisYearId, TicketStartDate = thisYear, TicketEndDate = thisYear.AddDays(1)},
            new(){TicketStartDate = nextYear, TicketEndDate = nextYear.AddDays(1)},
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.FilterTicketDatesBySelectedMonthsAndYear(query, thisYear.Year, null);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(1, resultingCollection.Count);
        Assert.AreEqual(thisYearId, resultingCollection[0].Id);
    }

   [TestMethod]
    public void IncludeOnDemandFilterIfApplicable_NullEndDateConditionTrue_ReturnsNone()
    {
        //setup
        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);

        var list = new List<LoadConfirmationEntity>
        {
            new(){EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = today, Status = LoadConfirmationStatus.Open},
            new(){EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = tomorrow, Status = LoadConfirmationStatus.Open},
            new(){EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = today, Status = LoadConfirmationStatus.Open},
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.IncludeOnDemandFilterIfApplicable(query, true, today);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(0, resultingCollection.Count);
    }

    [TestMethod]
    public void IncludeOnDemandFilterIfApplicable_NullEndDateConditionTrue_ReturnsYesterday()
    {
        //setup
        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);
        var todayId = new Guid("1EF4785B-EBC7-4BD7-985A-3C93D9360D31");
        var tomorrowId = new Guid("EE0D4FF6-75C3-4052-953F-20C55342C236");
        var yesterdayId = new Guid("BA54DFBF-396B-4FDF-BF4A-8DC69472959D");

        var list = new List<LoadConfirmationEntity>
        {
            new(){Id= todayId, EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = today, Status = LoadConfirmationStatus.Open},
            new(){Id= tomorrowId, EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = tomorrow, Status = LoadConfirmationStatus.Open},
            new(){Id = yesterdayId, EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = yesterday, Status = LoadConfirmationStatus.Open},
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.IncludeOnDemandFilterIfApplicable(query, true, today);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(1, resultingCollection.Count);
        Assert.AreEqual(yesterdayId, resultingCollection[0].Id);
    }

    [TestMethod]
    public void ApplyOnDemandDatesFilter_NullEndDateConditionTrue_ReturnsNone()
    {
        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);

        //setup
        var list = new List<LoadConfirmationEntity>
        {
            new(){EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = today, Status = LoadConfirmationStatus.Open},
            new(){EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = tomorrow, Status = LoadConfirmationStatus.Open},
            new(){EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = today, Status = LoadConfirmationStatus.Open},
        };

        var query = list.AsQueryable();

        //execute

        var resultingQuery = LoadConfirmationRepositoryHelper.ApplyOnDemandDatesFilter(query, today);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(0, resultingCollection.Count);
    }

    [TestMethod]
    public void ApplyOnDemandDatesFilter_NullEndDateConditionTrue_ReturnsYesterday()
    {
        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);
        var todayId = new Guid("1EF4785B-EBC7-4BD7-985A-3C93D9360D31");
        var tomorrowId = new Guid("EE0D4FF6-75C3-4052-953F-20C55342C236");
        var yesterdayId = new Guid("BA54DFBF-396B-4FDF-BF4A-8DC69472959D");

        //setup
        var list = new List<LoadConfirmationEntity>
        {
            new(){Id= todayId, EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = today},
            new(){Id= tomorrowId, EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = tomorrow},
            new(){Id = yesterdayId, EndDate = null, Frequency = LoadConfirmationFrequency.OnDemand.ToString(), StartDate = yesterday},
        };

        var query = list.AsQueryable();

        //execute

        var resultingQuery = LoadConfirmationRepositoryHelper.ApplyOnDemandDatesFilter(query, today);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(1, resultingCollection.Count);
        Assert.AreEqual(yesterdayId, resultingCollection[0].Id);
    }

    [TestMethod]
    public void ApplyOnDemandDatesFilter_NoEndDateConditionTrue_ReturnsNone()
    {
        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);

        //setup
        var list = new List<LoadConfirmationEntity>
        {
            new(){EndDate = today},
            new(){EndDate = tomorrow},
            new(){EndDate = today},
        };

        var query = list.AsQueryable();

        //execute

        var resultingQuery = LoadConfirmationRepositoryHelper.ApplyOnDemandDatesFilter(query, today);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(0, resultingCollection.Count);
    }

    [TestMethod]
    public void ApplyOnDemandDatesFilter_OneEndDateConditionTrue_ReturnsYesterday()
    {
        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);
        var todayId = Guid.NewGuid();
        var tomorrowId = Guid.NewGuid();
        var yesterdayId = Guid.NewGuid();

        //setup
        var list = new List<LoadConfirmationEntity>
        {
            new(){Id= todayId, EndDate = today},
            new(){Id=tomorrowId, EndDate = tomorrow},
            new(){Id = yesterdayId, EndDate = yesterday},
        };

        var query = list.AsQueryable();

        //execute

        var resultingQuery = LoadConfirmationRepositoryHelper.ApplyOnDemandDatesFilter(query, today);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(1, resultingCollection.Count);
        Assert.AreEqual(yesterdayId, resultingCollection[0].Id);
    }

    [TestMethod]
    public void ApplyOpenStatusFilter_ReturnsNone()
    {
        //setup
        var list = new List<LoadConfirmationEntity>
        {
            new(){Status = LoadConfirmationStatus.PendingSignature},
            new(){Status = LoadConfirmationStatus.PendingSignature},
            new(){Status = LoadConfirmationStatus.Rejected},
        };

        var query = list.AsQueryable();

        //execute

        var resultingQuery = LoadConfirmationRepositoryHelper.ApplyOpenStatusFilter(query);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(0, resultingCollection.Count);
    }

    [TestMethod]
    public void ApplyOpenStatusFilter_ReturnsOne()
    {
        //setup
        var list = new List<LoadConfirmationEntity>
        {
            new(){Status = LoadConfirmationStatus.PendingSignature},
            new(){Status = LoadConfirmationStatus.PendingSignature},
            new(){Status = LoadConfirmationStatus.Open},
        };

        var query = list.AsQueryable();

        //execute

        var resultingQuery = LoadConfirmationRepositoryHelper.ApplyOpenStatusFilter(query);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(1, resultingCollection.Count);
    }

    [TestMethod]
    public void IncludeOnDemandFilterIfApplicable_NoneInOpenStatus_ReturnsThree()
    {
        //setup
        DateTimeOffset currentAlbertaOffsetDate = DateTimeOffset.UtcNow;

        var list = new List<LoadConfirmationEntity>
        {
            new(){Status = LoadConfirmationStatus.PendingSignature},
            new(){Status = LoadConfirmationStatus.PendingSignature},
            new(){Status = LoadConfirmationStatus.Rejected},
        };

        var query = list.AsQueryable();

        //execute

        var resultingQuery = LoadConfirmationRepositoryHelper.IncludeOnDemandFilterIfApplicable(query, false, currentAlbertaOffsetDate);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(3, resultingCollection.Count);
    }

    [TestMethod]
    public void IncludeOnDemandFilterIfApplicable_FilterNotPresent_ReturnsThree()
    {
        //setup
        DateTimeOffset currentAlbertaOffsetDate = DateTimeOffset.UtcNow;

        SearchCriteria criteria = new SearchCriteria
        {
            Filters = new()
        };

        var list = new List<LoadConfirmationEntity>
        {
            new(){Status = LoadConfirmationStatus.PendingSignature},
            new(){Status = LoadConfirmationStatus.PendingSignature},
            new(){Status = LoadConfirmationStatus.Rejected},
        };

        var query = list.AsQueryable();

        //execute

        var resultingQuery = LoadConfirmationRepositoryHelper.IncludeOnDemandFilterIfApplicable(query, false, currentAlbertaOffsetDate);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(3, resultingCollection.Count);
    }

    [TestMethod]
    public void ExcludeVoidStatusIfApplicable_OnePostedStatus_ReturnsTwo()
    {
        //setup
        SearchCriteria criteria = new SearchCriteria
        {
            Filters = new() { { nameof(LoadConfirmationEntity.Status), "some irrelevant value" } }
        };

        var list = new List<LoadConfirmationEntity>
        {
            new(){Status = LoadConfirmationStatus.Open},
            new(){Status = LoadConfirmationStatus.Posted},
            new(){Status = LoadConfirmationStatus.Void},
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.ExcludePostedStatusIfApplicable(criteria, query);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(2, resultingCollection.Count);
    }

    [TestMethod]
    public void ExcludeVoidStatusIfApplicable_ReturnsThree()
    {
        //setup
        SearchCriteria criteria = new SearchCriteria
        {
            Filters = new() { { nameof(LoadConfirmationEntity.Status), "some irrelevant value" } }
        };

        var list = new List<LoadConfirmationEntity>
        {
            new(){Status = LoadConfirmationStatus.Open},
            new(){Status = LoadConfirmationStatus.PendingSignature},
            new(){Status = LoadConfirmationStatus.Rejected},
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.ExcludeVoidStatusIfApplicable(criteria, query);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(3, resultingCollection.Count);
    }

    [TestMethod]
    public void ExcludePostedStatusIfApplicable_OnePostedStatus_ReturnsTwo()
    {
        //setup
        SearchCriteria criteria = new SearchCriteria
        {
            Filters = new() { { nameof(LoadConfirmationEntity.Status), "some irrelevant value" } }
        };

        var list = new List<LoadConfirmationEntity>
        {
            new(){Status = LoadConfirmationStatus.Open},
            new(){Status = LoadConfirmationStatus.Posted},
            new(){Status = LoadConfirmationStatus.Void},
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.ExcludePostedStatusIfApplicable(criteria, query);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(2, resultingCollection.Count);
    }

    [TestMethod]
    public void ExcludePostedStatusIfApplicable_ReturnsThree()
    {
        //setup
        SearchCriteria criteria = new SearchCriteria
        {
            Filters = new() { { nameof(LoadConfirmationEntity.Status), "some irrelevant value" } }
        };

        var list = new List<LoadConfirmationEntity>
        {
            new(){Status = LoadConfirmationStatus.Open},
            new(){Status = LoadConfirmationStatus.PendingSignature},
            new(){Status = LoadConfirmationStatus.Void},
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.ExcludePostedStatusIfApplicable(criteria, query);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(3, resultingCollection.Count);
    }

    [TestMethod]
    public void FilterFrequenciesNotEqualToNoneIfApplicable_ReturnsTwo()
    {
        //setup
        SearchCriteria criteria = new SearchCriteria();

        var list = new List<LoadConfirmationEntity>
        {
            new(){Frequency = LoadConfirmationFrequency.Daily.ToString()},
            new(){Frequency = LoadConfirmationFrequency.None.ToString()},
            new(){Frequency = LoadConfirmationFrequency.OnDemand.ToString()},
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.ExcludeNoneFrequencyIfApplicable(criteria, query);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(2, resultingCollection.Count);
    }

    [TestMethod]
    public void FilterFrequenciesNotEqualToNoneIfApplicable_ReturnsThree()
    {
        //setup
        SearchCriteria criteria = new SearchCriteria
        {
            Filters = new() { { nameof(LoadConfirmationEntity.Frequency), "None" } }
        };

        var list = new List<LoadConfirmationEntity>
        {
            new(){Frequency = LoadConfirmationFrequency.None.ToString()},
            new(){Frequency = LoadConfirmationFrequency.None.ToString()},
            new(){Frequency = LoadConfirmationFrequency.None.ToString()},
        };

        var query = list.AsQueryable();

        //execute
        var resultingQuery = LoadConfirmationRepositoryHelper.ExcludeNoneFrequencyIfApplicable(criteria, query);
        var resultingCollection = resultingQuery.ToList();

        //assert
        Assert.AreEqual(3, resultingCollection.Count);
    }

    private class TestMonth
    {
        public DateTimeOffset FirstOfMonth { get; }
        public DateTimeOffset LastOfMonth { get; }

        public TestMonth(int month, int year)
        {
            FirstOfMonth = month.GetFirstDayOfMonth(year);
            LastOfMonth = month.GetLastDayOfMonth(year);
        }
    }

}
