using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.Invoices.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.Invoices;

[TestClass]
public class PostedInvoiceRelationStatusManagerTaskTests : TestScope<PostedInvoiceRelationStatusManagerTask>
{
    [TestMethod]
    public void Workflow_RunOrder_ShouldBeValid()
    {
        // arrange
        var scope = new DefaultScope();

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
    }

    [DataTestMethod]
    [DataRow(InvoiceStatus.UnPosted, InvoiceStatus.Posted, true)]
    [DataRow(InvoiceStatus.UnPosted, InvoiceStatus.AgingUnSent, true)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.Posted, false)]
    [DataRow(InvoiceStatus.Posted, InvoiceStatus.PostedRejected, false)]
    [DataRow(InvoiceStatus.Unknown, InvoiceStatus.Posted, false)]
    [DataRow(InvoiceStatus.Unknown, InvoiceStatus.AgingUnSent, false)]
    public async Task Workflow_ShouldRun_ExpectedTransitions(InvoiceStatus originalStatus, InvoiceStatus targetStatus, bool expectedResult)
    {
        // arrange
        var scope = new DefaultScope();

        var original = GenFu.GenFu.New<InvoiceEntity>();
        original.Status = originalStatus;
        var target = original.Clone();
        target.Status = targetStatus;

        var context = new BusinessContext<InvoiceEntity>(target, original);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().Be(expectedResult);
    }

    [TestMethod]
    public async Task Workflow_ShouldSetLoadConfirmations_Posted()
    {
        // arrange
        var scope = new DefaultScope();

        var original = GenFu.GenFu.New<InvoiceEntity>();
        original.Status = InvoiceStatus.UnPosted;
        var target = original.Clone();
        target.Status = InvoiceStatus.Posted;

        var context = new BusinessContext<InvoiceEntity>(target, original);

        var loadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>();
        loadConfirmations.ForEach(lc => lc.Status = LoadConfirmationStatus.WaitingForInvoice);
        var loadConfirmation = loadConfirmations.First();
        loadConfirmation.InvoiceId = original.Id;

        scope.LoadConfirmationProviderMock.SetupEntities(loadConfirmations);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        scope.LoadConfirmationProviderMock.Verify(p => p.Update(It.Is<LoadConfirmationEntity>(lc => lc.Id == loadConfirmation.Id && lc.Status == LoadConfirmationStatus.Posted), It.IsAny<bool>()),
                                                  Times.Once);
    }

    [TestMethod]
    public async Task Workflow_ShouldSetSalesLines_Posted()
    {
        // arrange
        var scope = new DefaultScope();

        var original = GenFu.GenFu.New<InvoiceEntity>();
        original.Status = InvoiceStatus.UnPosted;
        var target = original.Clone();
        target.Status = InvoiceStatus.Posted;

        var context = new BusinessContext<InvoiceEntity>(target, original);

        var salesLines = GenFu.GenFu.ListOf<SalesLineEntity>();
        salesLines.ForEach(sl => sl.Status = SalesLineStatus.Approved);
        var salesLine = salesLines.First();
        salesLine.InvoiceId = original.Id;

        scope.SalesLineProviderMock.SetupEntities(salesLines);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        scope.SalesLineProviderMock.Verify(p => p.Update(It.Is<SalesLineEntity>(sl => sl.Id == salesLine.Id && sl.Status == SalesLineStatus.Posted), It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task Workflow_ShouldSetTruckTickets_Invoice()
    {
        // arrange
        var scope = new DefaultScope();

        var original = GenFu.GenFu.New<InvoiceEntity>();
        original.Status = InvoiceStatus.UnPosted;
        var target = original.Clone();
        target.Status = InvoiceStatus.Posted;

        var context = new BusinessContext<InvoiceEntity>(target, original);

        var truckTickets = GenFu.GenFu.ListOf<TruckTicketEntity>();
        var truckTicket = truckTickets.First();

        var salesLines = GenFu.GenFu.ListOf<SalesLineEntity>();
        salesLines.ForEach(sl =>
                           {
                               sl.Status = SalesLineStatus.Approved;
                               sl.InvoiceId = original.Id;
                               sl.TruckTicketId = truckTicket.Id;
                           });

        scope.SalesLineProviderMock.SetupEntities(salesLines);
        scope.TruckTicketProviderMock.SetupEntities(truckTickets);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        scope.TruckTicketProviderMock.Verify(p => p.Update(It.Is<TruckTicketEntity>(tt => tt.Id == truckTicket.Id && tt.Status == TruckTicketStatus.Invoiced), It.IsAny<bool>()), Times.Once);
    }

    public class DefaultScope : TestScope<PostedInvoiceRelationStatusManagerTask>
    {
        public Mock<IProvider<Guid, LoadConfirmationEntity>> LoadConfirmationProviderMock = new();

        public Mock<IProvider<Guid, SalesLineEntity>> SalesLineProviderMock = new();

        public Mock<IProvider<Guid, TruckTicketEntity>> TruckTicketProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(LoadConfirmationProviderMock.Object,
                                    SalesLineProviderMock.Object,
                                    TruckTicketProviderMock.Object);
        }
    }
}
