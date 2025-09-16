using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.LoadConfirmation.Tasks;
using SE.Shared.Domain.Tests.TestUtilities;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.LoadConfirmations;

[TestClass]
public class LoadConfirmationGlInvoiceNumberHydrateTaskTests : TestScope<LoadConfirmationGlInvoiceNumberHydrateTask>
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
    public async Task Workflow_ShouldRun_InvoiceAssigned_InsertLoadConfirmation()
    {
        // arrange
        var scope = new DefaultScope();

        var targetEntity = GenFu.GenFu.New<LoadConfirmationEntity>();
        var invoiceEntity = GenFu.GenFu.New<InvoiceEntity>();
        targetEntity.InvoiceId = invoiceEntity.Id;

        var context = new BusinessContext<LoadConfirmationEntity>(targetEntity);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [DataTestMethod]
    public async Task Workflow_ShouldNotRun_InvoiceNotAssigned_InsertLoadConfirmation()
    {
        // arrange
        var scope = new DefaultScope();

        var targetEntity = GenFu.GenFu.New<LoadConfirmationEntity>();
        targetEntity.InvoiceId = default;

        var context = new BusinessContext<LoadConfirmationEntity>(targetEntity);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Workflow_ShouldNotRun_Invoice_NotUpdated()
    {
        // arrange
        var scope = new DefaultScope();

        var targetEntity = GenFu.GenFu.New<LoadConfirmationEntity>();
        var originalEntity = targetEntity.Clone();

        var invoiceEntity = GenFu.GenFu.New<InvoiceEntity>();
        targetEntity.InvoiceId = invoiceEntity.Id;
        originalEntity.InvoiceId = invoiceEntity.Id;

        var context = new BusinessContext<LoadConfirmationEntity>(targetEntity, originalEntity);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Workflow_ShouldRun_Invoice_Updated()
    {
        // arrange
        var scope = new DefaultScope();

        var targetEntity = GenFu.GenFu.New<LoadConfirmationEntity>();
        var originalEntity = targetEntity.Clone();

        var invoiceEntity = GenFu.GenFu.New<InvoiceEntity>();
        targetEntity.InvoiceId = invoiceEntity.Id;
        originalEntity.InvoiceId = Guid.NewGuid();

        var context = new BusinessContext<LoadConfirmationEntity>(targetEntity, originalEntity);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Workflow_ShouldUpdate_GlInvoiceNumber_OnLoadConfirmations()
    {
        // arrange
        var scope = new DefaultScope();

        var targetEntity = GenFu.GenFu.New<LoadConfirmationEntity>();
        var originalEntity = targetEntity.Clone();

        var invoiceEntity = GenFu.GenFu.New<InvoiceEntity>();
        targetEntity.InvoiceId = invoiceEntity.Id;
        originalEntity.InvoiceId = Guid.NewGuid();

        var context = new BusinessContext<LoadConfirmationEntity>(targetEntity, originalEntity);

        scope.InvoiceProviderMock.SetupEntities(new List<InvoiceEntity> { invoiceEntity });

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        Assert.AreEqual(targetEntity.GlInvoiceNumber, invoiceEntity.GlInvoiceNumber);
    }

    public class DefaultScope : TestScope<LoadConfirmationGlInvoiceNumberHydrateTask>
    {
        public Mock<IProvider<Guid, InvoiceEntity>> InvoiceProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(InvoiceProviderMock.Object);
        }
    }
}
