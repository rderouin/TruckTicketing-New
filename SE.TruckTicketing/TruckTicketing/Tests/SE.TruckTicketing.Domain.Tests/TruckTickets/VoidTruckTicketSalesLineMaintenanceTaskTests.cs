using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

[TestClass]
public class VoidTruckTicketSalesLineMaintenanceTaskTests
{
    [DataTestMethod]
    [DataRow(TruckTicketStatus.Open)]
    [DataRow(TruckTicketStatus.Hold)]
    [DataRow(TruckTicketStatus.Invoiced)]
    [DataRow(TruckTicketStatus.New)]
    [DataRow(TruckTicketStatus.Stub)]
    [DataRow(TruckTicketStatus.Approved)]
    public async Task Task_ShouldNotRun_ForNonVoid_TruckTicketStatuses(TruckTicketStatus invalidStatus)
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<TruckTicketEntity>();
        target.Status = invalidStatus;
        var context = new BusinessContext<TruckTicketEntity>(target);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_Void_Ticket()
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<TruckTicketEntity>();
        target.Status = TruckTicketStatus.Void;
        var context = new BusinessContext<TruckTicketEntity>(target);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldDeletePreviewSalesLines()
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<TruckTicketEntity>();
        target.Status = TruckTicketStatus.Void;
        var context = new BusinessContext<TruckTicketEntity>(target);

        var salesLines = GenFu.GenFu.ListOf<SalesLineEntity>();
        salesLines.ForEach(salesLine => salesLine.TruckTicketId = target.Id);
        var salesLine = salesLines.First();
        salesLine.Status = SalesLineStatus.Preview;
        scope.SalesLineProviderMock.SetupEntities(salesLines);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        scope.SalesLineProviderMock.Verify(p => p.Delete(It.Is<SalesLineEntity>(sl => sl.Id == salesLine.Id), true), Times.Once);
    }

    [DataTestMethod]
    [DataRow(SalesLineStatus.Approved)]
    [DataRow(SalesLineStatus.Exception)]
    [DataRow(SalesLineStatus.Posted)]
    [DataRow(SalesLineStatus.SentToFo)]
    public async Task Task_ShouldVoidOtherSalesLines(SalesLineStatus status)
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<TruckTicketEntity>();
        target.Status = TruckTicketStatus.Void;
        var context = new BusinessContext<TruckTicketEntity>(target);

        var salesLines = GenFu.GenFu.ListOf<SalesLineEntity>();
        salesLines.ForEach(salesLine => salesLine.TruckTicketId = target.Id);
        var salesLine = salesLines.First();
        salesLine.Status = SalesLineStatus.Approved;
        scope.SalesLineProviderMock.SetupEntities(salesLines);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        scope.SalesLineProviderMock.Verify(p => p.Update(It.Is<SalesLineEntity>(sl => sl.Id == salesLine.Id && sl.Status == SalesLineStatus.Void), true), Times.Once);
    }

    private class DefaultScope : TestScope<VoidTruckTicketSalesLineMaintenanceTask>
    {
        public readonly Mock<IProvider<Guid, SalesLineEntity>> SalesLineProviderMock = new();

        public readonly Mock<ISalesLinesPublisher> SalesLinePublisherMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(SalesLineProviderMock.Object, SalesLinePublisherMock.Object);
        }
    }
}
