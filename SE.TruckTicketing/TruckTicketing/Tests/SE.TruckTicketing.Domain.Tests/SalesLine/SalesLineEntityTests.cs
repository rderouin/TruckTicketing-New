using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain.Entities.SalesLine;

namespace SE.TruckTicketing.Domain.Tests.SalesLine;

[TestClass]
public class SalesLineEntityTests
{
    [TestMethod]
    public void ApplyFoRounding_GreaterThanFive()
    {
        //setup
        var salesLine = new SalesLineEntity { Id = Guid.Empty, Rate = 33.336, Quantity = 22.226};

        //execute
        salesLine.ApplyFoRounding();

        //assert
        Assert.IsNotNull(salesLine.Id);
        Assert.AreNotEqual(Guid.Empty, salesLine.Id);
        Assert.AreEqual(33.34, salesLine.Rate);
        Assert.AreEqual(22.23, salesLine.Quantity);
        Assert.AreEqual(741.15, salesLine.TotalValue);
    }

    [TestMethod]
    public void ApplyFoRounding_LessThanFive()
    {
        //setup
        var salesLine = new SalesLineEntity { Id = Guid.Empty, Rate = 33.334, Quantity = 22.224};

        //execute
        salesLine.ApplyFoRounding();

        //assert
        Assert.IsNotNull(salesLine.Id);
        Assert.AreNotEqual(Guid.Empty, salesLine.Id);
        Assert.AreEqual(33.33, salesLine.Rate);
        Assert.AreEqual(22.22, salesLine.Quantity);
        Assert.AreEqual(740.59, salesLine.TotalValue);
    }

    [TestMethod]
    public void ApplyFoRounding_EmptyGuidIdSetToNewGuid()
    {
        //setup
        var salesLine = new SalesLineEntity {Id = Guid.Empty};

        //execute
        salesLine.ApplyFoRounding();

        //assert
        Assert.IsNotNull(salesLine.Id);
        Assert.AreNotEqual(Guid.Empty, salesLine.Id);
    }
}
