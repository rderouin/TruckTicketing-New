using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.BillingConfigurations.Tasks;
using SE.TruckTicketing.Domain.Entities.SalesLine;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.BillingConfiguration;

[TestClass]
public class BillingConfigurationEdiReferenceUpdaterTaskTests
{
    [TestMethod]
    public void Task_RunParameters_AreValid()
    {
        // arrange / act
        var scope = new DefaultScope();

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
    }

    [TestMethod]
    public async Task Task_ShouldRun_UpdateNewEdi()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<BillingConfigurationEntity>();
        var target = original.Clone();
        target.EDIValueData = GenFu.GenFu.ListOf<EDIFieldValueEntity>(3);
        var context = new BusinessContext<BillingConfigurationEntity>(target, original) { Operation = Operation.Update };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_UpdateExistingEdi()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<BillingConfigurationEntity>();
        original.EDIValueData = GenFu.GenFu.ListOf<EDIFieldValueEntity>(3);
        var target = original.Clone();
        target.EDIValueData.First().EDIFieldValueContent += "Update";
        var context = new BusinessContext<BillingConfigurationEntity>(target, original) { Operation = Operation.Update };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_Insert()
    {
        // arrange
        var scope = new DefaultScope();
        var target = GenFu.GenFu.New<BillingConfigurationEntity>();
        var context = new BusinessContext<BillingConfigurationEntity>(target) { Operation = Operation.Insert };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_NoEdiUpdate()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<BillingConfigurationEntity>();
        var target = original.Clone();
        var context = new BusinessContext<BillingConfigurationEntity>(target, original) { Operation = Operation.Update };

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_Run_UpdatesSalesLineEdiValues()
    {
        // arrange
        var scope = new DefaultScope();
        var original = GenFu.GenFu.New<BillingConfigurationEntity>();
        var target = original.Clone();
        target.EDIValueData = GenFu.GenFu.ListOf<EDIFieldValueEntity>(3);
        var context = new BusinessContext<BillingConfigurationEntity>(target, original) { Operation = Operation.Update };

        var invoices = GenFu.GenFu.ListOf<InvoiceEntity>();
        var invoice = invoices.First();
        invoice.BillingConfigurationId = target.Id;
        invoice.Status = InvoiceStatus.UnPosted;
        scope.InvoiceManagerMock.SetupEntities(invoices);

        var truckTickets = GenFu.GenFu.ListOf<TruckTicketEntity>();
        var truckTicket = truckTickets.First();
        truckTicket.Status = TruckTicketStatus.Open;
        scope.TruckTicketProviderMock.SetupEntities(truckTickets);

        var salesLines = GenFu.GenFu.ListOf<SalesLineEntity>();
        var salesLine = salesLines.First();
        salesLine.TruckTicketId = truckTicket.Id;
        salesLine.InvoiceId = invoice.Id;
        salesLine.IsReversed = false;
        salesLine.IsReversal = false;
        scope.SalesLineProviderMock.SetupEntities(salesLines);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        scope.SalesLineProviderMock.Verify(p => p.Update(It.Is<SalesLineEntity>(sl => sl.Id == salesLine.Id &&
                                                                                      sl.EdiFieldValues.Select(e => e.EDIFieldValueContent)
                                                                                        .SequenceEqual(target.EDIValueData.Select(e => e.EDIFieldValueContent))), true), Times.Once);

        scope.TruckTicketProviderMock.Verify(p => p.Update(It.Is<TruckTicketEntity>(tt => tt.Id == truckTicket.Id &&
                                                                                          tt.EdiFieldValues.Select(e => e.EDIFieldValueContent)
                                                                                            .SequenceEqual(target.EDIValueData.Select(e => e.EDIFieldValueContent))), true), Times.Once);

        scope.SalesLinePublisherMock.Verify(p => p.PublishSalesLines(It.Is<IEnumerable<SalesLineEntity>>(lines => lines.Single().Id == salesLine.Id), It.IsAny<Operation>(), It.IsAny<bool>()),
                                            Times.Once);
    }

    private class DefaultScope : TestScope<BillingConfigEdiReferenceUpdaterTask>
    {
        public readonly Mock<IManager<Guid, InvoiceEntity>> InvoiceManagerMock = new();

        public readonly Mock<IProvider<Guid, SalesLineEntity>> SalesLineProviderMock = new();

        public readonly Mock<ISalesLinesPublisher> SalesLinePublisherMock = new();

        public readonly Mock<IProvider<Guid, TruckTicketEntity>> TruckTicketProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(TruckTicketProviderMock.Object,
                                    SalesLineProviderMock.Object,
                                    InvoiceManagerMock.Object,
                                    SalesLinePublisherMock.Object);
        }
    }
}
