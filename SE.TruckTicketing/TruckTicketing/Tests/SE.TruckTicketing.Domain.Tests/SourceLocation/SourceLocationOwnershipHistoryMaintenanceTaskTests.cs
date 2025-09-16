using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.SourceLocation.Tasks;

using Trident.Business;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.SourceLocation;

[TestClass]
public class SourceLocationOwnershipHistoryMaintenanceTaskTests : TestScope<SourceLocationOwnershipHistoryMaintenanceTask>
{
    [TestMethod]
    public void Task_ShouldRun_AfterValidation()
    {
        // arrange
        var scope = new DefaultScope();

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(Operation.Custom)]
    [DataRow(Operation.Delete)]
    [DataRow(Operation.Insert)]
    [DataRow(Operation.Undefined)]
    public async Task Task_ShouldNotRun_IfOperationIsNotUpdate(Operation operation)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Operation = operation;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_IfOperationIsUpdateAndGeneratorIdHasNotChanged()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext();
        context.Target.GeneratorId = context.Original.GeneratorId;
        context.Operation = Operation.Update;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_IfOperationIsUpdateAndGeneratorIdHasChanged()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateValidSourceLocationContext(scope.SourceLocation.Clone());
        context.Target.GeneratorId = Guid.NewGuid();
        context.Operation = Operation.Update;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldAddEntryToOwnershipHistory_IfOperationIsUpdateAndGeneratorIdHasChanged()
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.SourceLocation;
        var context = scope.CreateValidSourceLocationContext(original);
        context.Target.GeneratorId = Guid.NewGuid();
        context.Target.GeneratorStartDate = context.Original.GeneratorStartDate.AddMonths(1);
        context.Operation = Operation.Update;

        // act
        var shouldRun = await scope.InstanceUnderTest.Run(context);
        var previousOwner = context.Target.OwnershipHistory.Single();

        // assert
        shouldRun.Should().BeTrue();
        previousOwner.StartDate.Should().Be(original.GeneratorStartDate);
        previousOwner.EndDate.Should().BeCloseTo(context.Target.GeneratorStartDate, TimeSpan.FromMinutes(1));
        previousOwner.GeneratorId.Should().Be(original.GeneratorId);
        previousOwner.GeneratorAccountNumber.Should().Be(original.GeneratorAccountNumber);
        previousOwner.GeneratorName.Should().Be(original.GeneratorName);
    }

    private class DefaultScope : TestScope<SourceLocationOwnershipHistoryMaintenanceTask>
    {
        public readonly SourceLocationEntity SourceLocation =
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

        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<SourceLocationEntity> CreateValidSourceLocationContext(SourceLocationEntity original = null)
        {
            return new(SourceLocation, original ?? SourceLocation);
        }
    }
}
