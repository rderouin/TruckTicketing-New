using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Domain.Tasks;
using SE.TridentContrib.Extensions.Security;

using Trident.Business;
using Trident.Data;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.Shared.Domain.Tests.Tasks;

[TestClass]
public class TTAuditableEntityAuditTaskTests
{
    [TestMethod]
    public void WorkflowTask_CanBeInstantiated()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var operationStage = scope.InstanceUnderTest.Stage;

        // assert
        runOrder.Should().Be(-2);
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate | OperationStage.Custom);
    }

    [TestMethod]
    public async Task Workflow_ShouldSetUpdatedByProperties()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<TestEntity>(new());

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        context.Target.UpdatedBy.Should().Be(scope.TestUserContext.DisplayName);
        context.Target.UpdatedById.Should().Be(scope.TestUserContext.ObjectId);
        context.Target.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public async Task Workflow_ShouldSetCreatedByProperties_WhenOriginalEntityIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<TestEntity>(new());

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        context.Target.UpdatedBy.Should().Be(scope.TestUserContext.DisplayName);
        context.Target.UpdatedById.Should().Be(scope.TestUserContext.ObjectId);
        context.Target.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public async Task Workflow_ShouldMaintainCreatedByProperties_WhenOriginalEntityIsNotNull()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = new TestEntity
        {
            CreatedAt = DateTimeOffset.UtcNow.AddHours(1),
            CreatedBy = scope.TestUserContext.DisplayName + nameof(TestEntity),
            CreatedById = scope.TestUserContext.ObjectId + nameof(TestEntity),
        };

        var context = new BusinessContext<TestEntity>(new(), originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        context.Target.CreatedBy.Should().Be(originalEntity.CreatedBy);
        context.Target.CreatedById.Should().Be(originalEntity.CreatedById);
        context.Target.CreatedAt.Should().Be(originalEntity.CreatedAt);
    }

    [Container("Test", nameof(DocumentType), "Test", PartitionKeyType.WellKnown)]
    [Discriminator(nameof(EntityType), "Test")]
    private class TestEntity : TTAuditableEntityBase
    {
    }

    private class DefaultScope : TestScope<TTAuditableEntityAuditTask<TestEntity>>
    {
        public readonly UserContext TestUserContext = new()
        {
            DisplayName = "Jack Johnson",
            ObjectId = Guid.NewGuid().ToString(),
        };

        public readonly Mock<IUserContextAccessor> UserContextAccessorMock = new();

        public DefaultScope()
        {
            UserContextAccessorMock.Setup(m => m.UserContext).Returns(TestUserContext);

            InstanceUnderTest = new(UserContextAccessorMock.Object);
        }
    }
}
