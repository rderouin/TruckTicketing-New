using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Domain.Entities.SourceLocation;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.SourceLocation;

[TestClass]
public class SourceLocationIdentifierChangeMaterialApprovalReevaluateTaskTest : TestScope<SourceLocationIdentifierChangeMaterialApprovalReevaluateTask>
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task SourceLocationIdentifierChangeMaterialApprovalReevaluateTask_ShouldRun_Returns_True_When_OperationType_Is_GeneratorId_Update()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext(scope.DefaultSourceLocation.Clone());
        context.Target.GeneratorId = Guid.NewGuid();
        context.Operation = Operation.Update;

        //act
        var actual = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        Assert.IsTrue(actual);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SourceLocationIdentifierChangeMaterialApprovalReevaluateTask_ShouldRun_Returns_True_When_OperationType_Is_Identifier_Update()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext(scope.DefaultSourceLocation.Clone());
        context.Target.Identifier = "990/99-90-999-09W8/99";
        context.Operation = Operation.Update;

        //act
        var actual = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        Assert.IsTrue(actual);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SourceLocationIdentifierChangeMaterialApprovalReevaluateTask_ShouldRun_Returns_True_When_OperationType_Is_SourceLocationName_Update()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext(scope.DefaultSourceLocation.Clone());
        context.Target.SourceLocationName = "New Test";
        context.Operation = Operation.Update;

        //act
        var actual = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        Assert.IsTrue(actual);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SourceLocationIdentifierChangeMaterialApprovalReevaluateTask_ShouldRun_Returns_False_When_OperationType_Is_Identifier_And_GeneratorId_Or_Identifier_Not_Update()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Operation = Operation.Update;

        //act
        var actual = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        Assert.IsFalse(actual);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(Operation.Custom)]
    [DataRow(Operation.Delete)]
    [DataRow(Operation.Insert)]
    [DataRow(Operation.Undefined)]
    public async Task SourceLocationIdentifierChangeMaterialApprovalReevaluateTask_ShouldRun_Returns_False_When_OperationType_Is_Not_Update(Operation operation)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Operation = operation;

        //act
        var actual = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        Assert.IsFalse(actual);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SourceLocationIdentifierChangeMaterialApprovalReevaluateTask_Run_Execute_Successfully()
    {
        // arrange
        var scope = new DefaultScope();
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var context = scope.CreateValidSourceLocationContext();
        context.Operation = Operation.Update;
        scope.DefaultMaterialApproval.SourceLocationId = scope.DefaultSourceLocation.Id;
        scope.GetMaterialApprovalManagerSetup(scope.DefaultMaterialApproval);

        //act
        await scope.InstanceUnderTest.Run(context);
        var operationStage = scope.InstanceUnderTest.Stage;

        // assert
        runOrder.Should().BePositive();
        operationStage.Should().Be(OperationStage.PostValidation);
        scope.MaterialApprovalProviderMock.Verify(tt => tt.Update(It.IsAny<MaterialApprovalEntity>(),
                                                                  true), Times.AtLeastOnce);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task SourceLocationIdentifierChangeMaterialApprovalReevaluateTask_Run_Execute_No_MaterialApproval_Update()
    {
        // arrange
        var scope = new DefaultScope();
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var context = scope.CreateValidSourceLocationContext();
        context.Operation = Operation.Update;
        scope.GetMaterialApprovalManagerSetup();

        //act
        await scope.InstanceUnderTest.Run(context);
        var operationStage = scope.InstanceUnderTest.Stage;

        // assert
        runOrder.Should().BePositive();
        operationStage.Should().Be(OperationStage.PostValidation);
        scope.MaterialApprovalProviderMock.Verify(tt => tt.Update(It.IsAny<MaterialApprovalEntity>(),
                                                                  true), Times.Never);
    }

    private class DefaultScope : TestScope<SourceLocationIdentifierChangeMaterialApprovalReevaluateTask>
    {
        public readonly SourceLocationEntity DefaultSourceLocation =
            new()
            {
                Id = Guid.NewGuid(),
                Identifier = "990/99-90-999-09W9/99",
                FormattedIdentifier = "990999099909W999",
                GeneratorId = Guid.NewGuid(),
                GeneratorStartDate = DateTimeOffset.Now,
                ContractOperatorId = Guid.NewGuid(),
                GeneratorAccountNumber = "12345",
                GeneratorName = "Test",
            };

        public readonly MaterialApprovalEntity DefaultMaterialApproval =
            new()
            {
                Id = Guid.NewGuid(),
                GeneratorId = Guid.NewGuid(),
                GeneratorName = "Test",
                SourceLocationId = Guid.NewGuid(),
                SourceLocation = "990/99-90-999-09W9/99",
                SourceLocationFormattedIdentifier = "990/99-90-999-09W9/99",
                SourceLocationUnformattedIdentifier = "990999099909W999",
            };

        public DefaultScope()
        {
            InstanceUnderTest = new(MaterialApprovalProviderMock.Object);
        }

        public Mock<IProvider<Guid, MaterialApprovalEntity>> MaterialApprovalProviderMock { get; } = new();

        public void GetMaterialApprovalManagerSetup(params MaterialApprovalEntity[] materialApprovalEntities)
        {
            MaterialApprovalProviderMock.SetupEntities(materialApprovalEntities);
        }

        public BusinessContext<SourceLocationEntity> CreateValidSourceLocationContext(SourceLocationEntity original = null)
        {
            return new(DefaultSourceLocation, original ?? DefaultSourceLocation);
        }
    }
}
