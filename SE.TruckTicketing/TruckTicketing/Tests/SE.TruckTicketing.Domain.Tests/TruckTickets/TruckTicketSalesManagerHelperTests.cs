using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

[TestClass]
public class TruckTicketSalesManagerHelperTests
{
    // should i make another helper method to test populating a lc data on each sales lines? plus the updating of the LC ticket start dates and end dates?
    
    [TestMethod]
    public void WasTicketJustApproved_NewStatusApproved_ExistingStatusHold_ReturnsTrue()
    {
        //setup
        TruckTicketEntity existingTicket = new TruckTicketEntity() { Status = TruckTicketStatus.Hold };
        TruckTicketEntity newTicket = new TruckTicketEntity(){Status = TruckTicketStatus.Approved};

        //execute
        var result = TruckTicketSalesManagerHelper.WasTicketJustApproved(existingTicket, newTicket);

        //assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void WasTicketJustApproved_NewStatusApproved_ExistingStatusOpen_ReturnsTrue()
    {
        //setup
        TruckTicketEntity existingTicket = new TruckTicketEntity() { Status = TruckTicketStatus.Open };
        TruckTicketEntity newTicket = new TruckTicketEntity(){Status = TruckTicketStatus.Approved};

        //execute
        var result = TruckTicketSalesManagerHelper.WasTicketJustApproved(existingTicket, newTicket);

        //assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void WasTicketJustApproved_NewStatusApproved_ExistingStatusVoid_ReturnsFalse()
    {
        //setup
        TruckTicketEntity existingTicket = new TruckTicketEntity() { Status = TruckTicketStatus.Void };
        TruckTicketEntity newTicket = new TruckTicketEntity(){Status = TruckTicketStatus.Approved};

        //execute
        var result = TruckTicketSalesManagerHelper.WasTicketJustApproved(existingTicket, newTicket);

        //assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void WasTicketJustApproved_NewTicketStatusNew_ReturnsFalse()
    {
        //setup
        TruckTicketEntity existingTicket = new TruckTicketEntity() { Status = TruckTicketStatus.Hold };
        TruckTicketEntity newTicket = new TruckTicketEntity(){Status = TruckTicketStatus.New};

        //execute
        var result = TruckTicketSalesManagerHelper.WasTicketJustApproved(existingTicket, newTicket);

        //assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void UpdateTruckTicketFromSalesLines_IsAdditionalServiceFalse_ReturnsOneAndSanitizesValuesAndSetsTtEffDate()
    {
        //setup
        var today = DateTime.Today;
        var yesterday = DateTime.Today.AddDays(-1);

        var truckTicket = new TruckTicketEntity() { EffectiveDate = today };
        List<SalesLineEntity> salesLines = new List<SalesLineEntity>()
        {
            new(){Rate = 3.333, Quantity = 5.555, TotalValue = 8.888, IsAdditionalService = false, TruckTicketEffectiveDate = yesterday}
        };

        //execute
        TruckTicketSalesManagerHelper.UpdateTruckTicketFromSalesLines(truckTicket, salesLines);

        //assert
        Assert.AreEqual(0, truckTicket.AdditionalServicesQty);
        Assert.AreNotEqual(Guid.Empty, salesLines[0].Id);
        Assert.AreEqual(yesterday, salesLines[0].TruckTicketEffectiveDate);
        Assert.AreEqual(3.33, salesLines[0].Rate);
        Assert.AreEqual(5.56, salesLines[0].Quantity);
        Assert.AreEqual(18.51, salesLines[0].TotalValue);
    }

    [TestMethod]
    public void UpdateTruckTicketFromSalesLines_IsAdditionalServiceTrue_ReturnsOneAndSanitizesValuesAndSetsTtEffDate()
    {
        //setup
        var today = DateTime.Today;

        var truckTicket = new TruckTicketEntity() { EffectiveDate = today };
        List<SalesLineEntity> salesLines = new List<SalesLineEntity>()
        {
            new(){Rate = 3.333, Quantity = 5.555, TotalValue = 8.888, IsAdditionalService = true}
        };

        //execute
        TruckTicketSalesManagerHelper.UpdateTruckTicketFromSalesLines(truckTicket, salesLines);

        //assert
        Assert.AreEqual(1, truckTicket.AdditionalServicesQty);
        Assert.AreEqual(today, salesLines[0].TruckTicketEffectiveDate);
        Assert.AreEqual(3.33, salesLines[0].Rate);
        Assert.AreEqual(5.56, salesLines[0].Quantity);
        Assert.AreEqual(18.51, salesLines[0].TotalValue);
    }

    [TestMethod]
    public void ShouldUpdateSalesLines_StatusStubHasSalesLinesAndTicketNumber_ReturnsTrue()
    {
        //setup
        TruckTicketEntity truckTicket = new TruckTicketEntity(){Status = TruckTicketStatus.Stub, TicketNumber = "test"};
        var salesLines = new List<SalesLineEntity>()
        {
            new()
        };

        //execute
        var result = TruckTicketSalesManagerHelper.ShouldUpdateSalesLines(truckTicket, salesLines);

        //assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldUpdateSalesLines_NullTicketNumber_ReturnsFalse()
    {
        //setup
        TruckTicketEntity truckTicket = new TruckTicketEntity(){Status = TruckTicketStatus.Stub, TicketNumber = null};
        var salesLines = new List<SalesLineEntity>()
        {
            new()
        };

        //execute
        var result = TruckTicketSalesManagerHelper.ShouldUpdateSalesLines(truckTicket, salesLines);

        //assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldUpdateSalesLines_ZeroSalesLines_ReturnsFalse()
    {
        //setup
        TruckTicketEntity truckTicket = new TruckTicketEntity(){Status = TruckTicketStatus.Stub, TicketNumber = "test"};
        var salesLines = new List<SalesLineEntity> {};

        //execute
        var result = TruckTicketSalesManagerHelper.ShouldUpdateSalesLines(truckTicket, salesLines);

        //assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldUpdateSalesLines_StatusVoid_ReturnsFalse()
    {
        //setup
        TruckTicketEntity truckTicket = new TruckTicketEntity(){Status = TruckTicketStatus.Void, TicketNumber = "test"};
        var salesLines = new List<SalesLineEntity>()
        {
            new(),
            new()
        };

        //execute
        var result = TruckTicketSalesManagerHelper.ShouldUpdateSalesLines(truckTicket, salesLines);

        //assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryTransitioningTicketToOpenStatus_NewStatus_SetsToToOpen()
    {
        //setup
        TruckTicketEntity truckTicket = new TruckTicketEntity { Status = TruckTicketStatus.New };

        //execute
        TruckTicketSalesManagerHelper.TryTransitioningTicketToOpenStatus(truckTicket);

        //assert
        Assert.AreEqual(TruckTicketStatus.Open, truckTicket.Status);
    }
    
    [TestMethod]
    public void TryTransitioningTicketToOpenStatus_StubStatus_SetsToToOpen()
    {
        //setup
        TruckTicketEntity truckTicket = new TruckTicketEntity { Status = TruckTicketStatus.Stub };

        //execute
        TruckTicketSalesManagerHelper.TryTransitioningTicketToOpenStatus(truckTicket);

        //assert
        Assert.AreEqual(TruckTicketStatus.Open, truckTicket.Status);
    }
    
    [TestMethod]
    public void TryTransitioningTicketToOpenStatus_VoidStatus_DoesNotSetToToOpen()
    {
        //setup
        TruckTicketEntity truckTicket = new TruckTicketEntity { Status = TruckTicketStatus.Void };

        //execute
        TruckTicketSalesManagerHelper.TryTransitioningTicketToOpenStatus(truckTicket);

        //assert
        Assert.AreEqual(TruckTicketStatus.Void, truckTicket.Status);
    }


}
