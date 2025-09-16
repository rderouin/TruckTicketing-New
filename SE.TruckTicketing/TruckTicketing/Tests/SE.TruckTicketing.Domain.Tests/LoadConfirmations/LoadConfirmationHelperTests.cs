using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.SalesLine;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Domain.Tests.LoadConfirmations;
[TestClass]
public class LoadConfirmationHelperTests
{
    [TestMethod]
    public void UpdateEffectiveDateRange_Jan02StartDate_Jan05EndDate_VoidIsIgnored()
    {
        //setup
        DateTimeOffset january01 = new DateTimeOffset(2023, 1, 1, 1, 0, 0, TimeSpan.Zero);
        DateTimeOffset january02 = new DateTimeOffset(2023, 1, 2, 1, 0, 0, TimeSpan.Zero);
        DateTimeOffset january03 = new DateTimeOffset(2023, 1, 3, 1, 0, 0, TimeSpan.Zero);
        DateTimeOffset january04 = new DateTimeOffset(2023, 1, 4, 1, 0, 0, TimeSpan.Zero);
        DateTimeOffset january05 = new DateTimeOffset(2023, 1, 5, 1, 0, 0, TimeSpan.Zero);//one week difference

        var lc = new LoadConfirmationEntity();
        List<SalesLineEntity> salesLines = new List<SalesLineEntity>
        {
            new(){ Status = SalesLineStatus.Void, TruckTicketEffectiveDate = january01.DateTime},
            new(){ Status = SalesLineStatus.Approved, TruckTicketEffectiveDate = january02.DateTime},
            new(){ Status = SalesLineStatus.Exception, TruckTicketEffectiveDate = january03.DateTime},
            new(){ Status = SalesLineStatus.Preview, TruckTicketEffectiveDate = january04.DateTime},
            new(){ Status = SalesLineStatus.Posted, TruckTicketEffectiveDate = january05.DateTime},
        };

        //execute
        LoadConfirmationHelper.SetLoadConfirmationTicketStartEndDates(lc, salesLines);

        //assert

        AssertDateTimes(january02, lc.TicketStartDate);
        AssertDateTimes(january05, lc.TicketEndDate);

    }

    [TestMethod]
    public void UpdateEffectiveDateRange_Jan01StartDate_Jan05EndDate()
    {
        //setup
        DateTimeOffset january01 = new DateTimeOffset(2023, 1, 1, 1, 0, 0, TimeSpan.Zero);
        DateTimeOffset january02 = new DateTimeOffset(2023, 1, 2, 1, 0, 0, TimeSpan.Zero);
        DateTimeOffset january03 = new DateTimeOffset(2023, 1, 3, 1, 0, 0, TimeSpan.Zero);
        DateTimeOffset january04 = new DateTimeOffset(2023, 1, 4, 1, 0, 0, TimeSpan.Zero);
        DateTimeOffset january05 = new DateTimeOffset(2023, 1, 5, 1, 0, 0, TimeSpan.Zero);//one week difference

        var lc = new LoadConfirmationEntity();
        List<SalesLineEntity> salesLines = new List<SalesLineEntity>
        {
            new(){ Status = SalesLineStatus.Approved, TruckTicketEffectiveDate = january01.DateTime},
            new(){ Status = SalesLineStatus.Void, TruckTicketEffectiveDate = january02.DateTime},
            new(){ Status = SalesLineStatus.Exception, TruckTicketEffectiveDate = january03.DateTime},
            new(){ Status = SalesLineStatus.Preview, TruckTicketEffectiveDate = january04.DateTime},
            new(){ Status = SalesLineStatus.Posted, TruckTicketEffectiveDate = january05.DateTime},
        };

        //execute
        LoadConfirmationHelper.SetLoadConfirmationTicketStartEndDates(lc, salesLines);

        //assert

        AssertDateTimes(january01, lc.TicketStartDate);
        AssertDateTimes(january05, lc.TicketEndDate);

    }

    private static void AssertDateTimes(DateTimeOffset expectedDateTimeOffset, DateTimeOffset actualDateTime)
    {
        Assert.AreEqual(expectedDateTimeOffset.DateTime, actualDateTime);
    }
}
