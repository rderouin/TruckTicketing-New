using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Domain.Entities.Invoices.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.Invoices;

[TestClass]
public class InvoiceGLNumberReferenceToLoadConfirmationTaskTests : TestScope<PostedInvoiceRelationStatusManagerTask>
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
    public async Task Workflow_ShouldRun_GlNumberPresent_InsertInvoice()
    {
        // arrange
        var scope = new DefaultScope();

        var targetEntity = GenFu.GenFu.New<InvoiceEntity>();
        var loadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        loadConfirmations.ForEach(x => x.InvoiceId = targetEntity.Id);

        var context = new BusinessContext<InvoiceEntity>(targetEntity);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [DataTestMethod]
    public async Task Workflow_ShouldNotRun_GlNumberNotPresent_InsertInvoice()
    {
        // arrange
        var scope = new DefaultScope();

        var targetEntity = GenFu.GenFu.New<InvoiceEntity>();
        targetEntity.GlInvoiceNumber = string.Empty;
        var loadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        loadConfirmations.ForEach(x => x.InvoiceId = targetEntity.Id);

        var context = new BusinessContext<InvoiceEntity>(targetEntity);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Workflow_ShouldNotRun_GlNumber_NotUpdated()
    {
        // arrange
        var scope = new DefaultScope();

        var targetEntity = GenFu.GenFu.New<InvoiceEntity>();
        targetEntity.GlInvoiceNumber = string.Empty;
        var loadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        loadConfirmations.ForEach(x => x.InvoiceId = targetEntity.Id);

        var context = new BusinessContext<InvoiceEntity>(targetEntity, targetEntity.Clone());

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Workflow_ShouldRun_GlNumber_Updated()
    {
        // arrange
        var scope = new DefaultScope();

        var targetEntity = GenFu.GenFu.New<InvoiceEntity>();
        var originalEntity = targetEntity.Clone();
        originalEntity.GlInvoiceNumber = string.Empty;
        var loadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        loadConfirmations.ForEach(x => x.InvoiceId = targetEntity.Id);

        var context = new BusinessContext<InvoiceEntity>(targetEntity, originalEntity);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Workflow_ShouldUpdateInvoiceGlNumber_OnLoadConfirmations()
    {
        // arrange
        var scope = new DefaultScope();

        var original = GenFu.GenFu.New<InvoiceEntity>();
        original.GlInvoiceNumber = string.Empty;
        var target = original.Clone();

        var context = new BusinessContext<InvoiceEntity>(target, original);

        var loadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        loadConfirmations.ForEach(lc => lc.InvoiceId = target.Id);
        var loadConfirmation = loadConfirmations.First();
        loadConfirmation.InvoiceId = original.Id;

        scope.LoadConfirmationProviderMock.SetupEntities(loadConfirmations);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        scope.LoadConfirmationProviderMock.Verify(p => p.Update(It.Is<LoadConfirmationEntity>(lc => lc.Id == loadConfirmation.Id && lc.GlInvoiceNumber == target.GlInvoiceNumber), It.IsAny<bool>()),
                                                  Times.Once);
    }

    public class DefaultScope : TestScope<InvoiceGLNumberReferenceToLoadConfirmationTask>
    {
        public Mock<IProvider<Guid, LoadConfirmationEntity>> LoadConfirmationProviderMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(LoadConfirmationProviderMock.Object);
        }
    }
}
