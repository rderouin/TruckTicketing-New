using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.SalesLine;

[TestClass]
public class SalesLineAggregatorTaskTests
{
    private static BusinessContext<SalesLineEntity> GetApprovedSalesLineContext()
    {
        var original = GenFu.GenFu.New<SalesLineEntity>();
        original.Status = SalesLineStatus.Approved;
        original.IsReversal = false;
        original.IsReversed = false;

        var target = original.Clone();
        return new(target, original);
    }

    [TestMethod]
    public void Workflow_RunOrder_ShouldBeConfiguredCorrectly()
    {
        // arrange
        var scope = new DefaultScope();

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
    }

    [TestMethod]
    public async Task Workflow_ShouldNotRun_WhenThereIsNoLCOrInvoiceChangeOrValueChange()
    {
        // arrange
        var scope = new DefaultScope();
        var context = GetApprovedSalesLineContext();

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Workflow_ShouldNotRun_IfSalesLineIsReversed()
    {
        // arrange
        var scope = new DefaultScope();
        var context = GetApprovedSalesLineContext();
        context.Target.IsReversed = true;

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Workflow_ShouldNotRun_IfSalesLineIsReversal()
    {
        // arrange
        var scope = new DefaultScope();
        var context = GetApprovedSalesLineContext();
        context.Target.IsReversal = true;
        context.Target.TotalValue *= -1;

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Workflow_ShouldRun_WhenThereIsAnInvoiceAssignmentChange()
    {
        // arrange
        var scope = new DefaultScope();
        var context = GetApprovedSalesLineContext();
        context.Original.InvoiceId = Guid.NewGuid();
        context.Target.InvoiceId = Guid.NewGuid();

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Workflow_ShouldRun_WhenThereIsALoadConfirmationAssignmentChange()
    {
        // arrange
        var scope = new DefaultScope();
        var context = GetApprovedSalesLineContext();
        context.Original.LoadConfirmationId = Guid.NewGuid();
        context.Target.LoadConfirmationId = Guid.NewGuid();

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Workflow_ShouldRun_WhenThereIsAValueChange()
    {
        // arrange
        var scope = new DefaultScope();
        var context = GetApprovedSalesLineContext();
        context.Operation = Operation.Update;
        context.Original.TotalValue = 10;
        context.Target.TotalValue = 20;

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Workflow_Should_AddSalesLinesToTargetAggregates_WhenValueHasChanged_ForSameInvoice()
    {
        // arrange
        var scope = new DefaultScope();

        var invoice = new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            SalesLineCount = 1,
        };

        scope.InvoiceProviderMock.SetupEntities(new[] { invoice });

        var loadConfirmation = new LoadConfirmationEntity
        {
            Id = Guid.NewGuid(),
            SalesLineCount = 1,
        };

        scope.LoadConfirmationProviderMock.SetupEntities(new[] { loadConfirmation });

        var context = GetApprovedSalesLineContext();
        context.Operation = Operation.Update;
        context.Original.TotalValue = 10;
        context.Original.InvoiceId = invoice.Id;
        context.Original.LoadConfirmationId = loadConfirmation.Id;

        context.Target.TotalValue = 20;
        context.Target.InvoiceId = invoice.Id;
        context.Target.LoadConfirmationId = loadConfirmation.Id;

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        invoice.InvoiceAmount.Should().Be(10);
        invoice.SalesLineCount.Should().Be(1);

        loadConfirmation.TotalCost.Should().Be(10);
        loadConfirmation.SalesLineCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Workflow_Should_AddSalesLinesToTargetAggregates_When_LoadConfirmationAndInvoiceHasChanged()
    {
        // arrange
        var scope = new DefaultScope();

        var newInvoice = new InvoiceEntity { Id = Guid.NewGuid() };
        scope.InvoiceProviderMock.SetupEntities(new[] { newInvoice });

        var newLoadConfirmation = new LoadConfirmationEntity { Id = Guid.NewGuid() };
        scope.LoadConfirmationProviderMock.SetupEntities(new[] { newLoadConfirmation });

        var context = GetApprovedSalesLineContext();
        context.Operation = Operation.Update;

        context.Target.TotalValue = 10;
        context.Target.InvoiceId = newInvoice.Id;
        context.Target.LoadConfirmationId = newLoadConfirmation.Id;

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        newInvoice.InvoiceAmount.Should().Be(10);
        newInvoice.SalesLineCount.Should().Be(1);

        newLoadConfirmation.TotalCost.Should().Be(10);
        newLoadConfirmation.SalesLineCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Workflow_Should_UpdateSalesLinesToTargetAggregates_When_LoadConfirmationAndInvoiceHasChanged()
    {
        // arrange
        var scope = new DefaultScope();

        var oldInvoice = new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            SalesLineCount = 1,
            InvoiceAmount = 10,
        };

        var newInvoice = new InvoiceEntity { Id = Guid.NewGuid() };
        scope.InvoiceProviderMock.SetupEntities(new[] { oldInvoice, newInvoice });

        var oldLoadConfirmation = new LoadConfirmationEntity
        {
            Id = Guid.NewGuid(),
            SalesLineCount = 1,
            TotalCost = 10,
        };

        var newLoadConfirmation = new LoadConfirmationEntity { Id = Guid.NewGuid() };
        scope.LoadConfirmationProviderMock.SetupEntities(new[] { oldLoadConfirmation, newLoadConfirmation });

        var context = GetApprovedSalesLineContext();
        context.Operation = Operation.Update;
        context.Original.TotalValue = 10;
        context.Original.InvoiceId = oldInvoice.Id;
        context.Original.LoadConfirmationId = oldLoadConfirmation.Id;

        context.Target.TotalValue = 10;
        context.Target.InvoiceId = newInvoice.Id;
        context.Target.LoadConfirmationId = newLoadConfirmation.Id;

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        oldInvoice.InvoiceAmount.Should().Be(0);
        oldInvoice.SalesLineCount.Should().Be(0);

        newInvoice.InvoiceAmount.Should().Be(10);
        newInvoice.SalesLineCount.Should().Be(1);

        newLoadConfirmation.TotalCost.Should().Be(10);
        newLoadConfirmation.SalesLineCount.Should().Be(1);

        oldLoadConfirmation.TotalCost.Should().Be(0);
        oldLoadConfirmation.SalesLineCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Workflow_Should_RemoveSalesLinesToTargetAggregates_When_LoadConfirmationAndInvoiceIsRemovedd()
    {
        // arrange
        var scope = new DefaultScope();

        var oldInvoice = new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            SalesLineCount = 1,
            InvoiceAmount = 10,
        };

        scope.InvoiceProviderMock.SetupEntities(new[] { oldInvoice });

        var oldLoadConfirmation = new LoadConfirmationEntity
        {
            Id = Guid.NewGuid(),
            SalesLineCount = 1,
            TotalCost = 10,
        };

        scope.LoadConfirmationProviderMock.SetupEntities(new[] { oldLoadConfirmation });

        var context = GetApprovedSalesLineContext();
        context.Operation = Operation.Update;
        context.Original.TotalValue = 10;
        context.Original.InvoiceId = oldInvoice.Id;
        context.Original.LoadConfirmationId = oldLoadConfirmation.Id;

        context.Target.TotalValue = 10;
        context.Target.InvoiceId = default;
        context.Target.LoadConfirmationId = default;

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        oldInvoice.InvoiceAmount.Should().Be(0);
        oldInvoice.SalesLineCount.Should().Be(0);

        oldLoadConfirmation.TotalCost.Should().Be(0);
        oldLoadConfirmation.SalesLineCount.Should().Be(0);
    }

    public class DefaultScope : TestScope<SalesLineAggregatorTask>
    {
        public Mock<IProvider<Guid, InvoiceEntity>> InvoiceProviderMock = new();

        public Mock<IProvider<Guid, LoadConfirmationEntity>> LoadConfirmationProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(LoadConfirmationProviderMock.Object,
                                    InvoiceProviderMock.Object);
        }
    }
}
