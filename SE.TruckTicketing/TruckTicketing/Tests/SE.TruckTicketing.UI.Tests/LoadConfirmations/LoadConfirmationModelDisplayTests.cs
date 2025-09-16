using System;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.TruckTicketing.Contracts.Models.LoadConfirmations;

namespace SE.TruckTicketing.UI.Tests.LoadConfirmations;
[TestClass]
public class LoadConfirmationModelDisplayTests
{
    [TestMethod]
    public void TicketDateRange_SameDatesDisplayCorrectly()
    {
        //setup
        const int day = 1;
        var testDate = CreateJanuaryTestDate(day);
        var lc = new LoadConfirmation { TicketStartDate = testDate, TicketEndDate = testDate };

        //execute
        var result = lc.TicketDateRange;

        //assert
        Assert.AreEqual("1/1/2023-1/1/2023", result);
    }

    [TestMethod]
    public void TicketDateRange_2Dates_DisplayCorrectly()
    {
        //setup
        var january01 = CreateJanuaryTestDate(1);
        var january03 = CreateJanuaryTestDate(3);

        var lc = new LoadConfirmation { TicketStartDate = january01, TicketEndDate = january03 };

        //execute
        var result = lc.TicketDateRange;

        //assert
        Assert.AreEqual("1/1/2023-1/3/2023", result);
    }

    private static DateTime CreateJanuaryTestDate(int day)
    {
        MimicUiConfigSetting();
        return new(2023, 1, day, 1, 0, 0, DateTimeKind.Local);
    }

    private static void MimicUiConfigSetting()
    {
        //why this? Apparently we use Umbraco that sets this setting on the client:
        //...SE.TruckTicketing\TruckTicketing\Hosts\SE.TruckTicketing.Client\Configuration\appsettings-schema.json -->line 1037 determines the UI date display format
        Thread.CurrentThread.CurrentCulture = new("en-US");
    }
}

