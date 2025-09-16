using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Tasks;

using Trident.Business;
using Trident.Contracts.Configuration;
using Trident.Data;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.Shared.Domain.Tests.Tasks;

[TestClass]
public class EntityUpdatePublisherTasksTests
{
    [DataTestMethod]
    [DataRow(Operation.Delete)]
    [DataRow(Operation.Insert)]
    [DataRow(Operation.Update)]
    public async Task Task_ShouldRun_WhenSettingsCanBeFoundForDefaultConfig(Operation operation)
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<SampleEntity>();
        var context = new BusinessContext<SampleEntity>(entity) { Operation = operation };
        scope.ConfigureSettings(entity.EntityType);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.AfterDelete | OperationStage.AfterInsert | OperationStage.AfterUpdate);
        shouldRun.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow(Operation.Delete)]
    [DataRow(Operation.Insert)]
    [DataRow(Operation.Update)]
    public async Task Task_ShouldNotRun_WhenSettingsCantBeFound(Operation operation)
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<SampleEntity>();
        var context = new BusinessContext<SampleEntity>(entity) { Operation = operation };
        scope.ConfigureSettings("randomEntityType");

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldPublishEntity_WhenRan()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<SampleEntity>();
        var context = new BusinessContext<SampleEntity>(entity) { Operation = Operation.Update };
        scope.ConfigureSettings(entity.EntityType);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
    }

    public class DefaultScope : TestScope<EntityUpdatePublisherTask<SampleEntity>>
    {
        public EntityEnvelopeModel<SampleEntity> PublishedEntity;

        public DefaultScope()
        {
            InstanceUnderTest = new(AppSettingsMock.Object, EntityPublisherMock.Object);
        }

        public Mock<IAppSettings> AppSettingsMock { get; } = new();

        public Mock<IEntityPublisher> EntityPublisherMock { get; } = new();

        public void ConfigureSettings(string entityType)
        {
            var settings = new EntityUpdatesPublisherSettings { PublishEntities = new[] { new EntityUpdatesPublisherSettings.EntityPublishSettings { EntityType = entityType } } };

            AppSettingsMock.Setup(settings => settings.GetSection<EntityUpdatesPublisherSettings>(It.IsAny<string>()))
                           .Returns(settings);
        }
    }

    [UseSharedDataSource("TestDB")]
    [Container("ContainerName", nameof(DocumentType), "Samples", PartitionKeyType.WellKnown)]
    [Discriminator(nameof(EntityType), "Sample")]
    public class SampleEntity : TTAuditableEntityBase
    {
        public new string EntityType { get; set; } = "Sample";
    }
}
