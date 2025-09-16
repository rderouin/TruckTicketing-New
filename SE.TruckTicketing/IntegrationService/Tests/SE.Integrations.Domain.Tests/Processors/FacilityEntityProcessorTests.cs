using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.LegalEntity;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class FacilityEntityProcessorTests
{
    private readonly Mock<IProvider<Guid, LegalEntityEntity>> _legalEntityprovider = new();

    private FacilityEntityProcessor _entityProcessor = null!;

    private Mock<ILog> _log = null!;

    private Mock<IManager<Guid, FacilityEntity>> _manager = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();
        _log = new();
        _entityProcessor = new(_serviceMapperRegistry, _manager.Object!, _log.Object!, _legalEntityprovider.Object!);
    }

    [TestMethod("FacilityEntityProcessor should be able to process a normal message.")]
    public async Task FacilityEntityProcessor_Process_NormalMessage()
    {
        // arrange
        FacilityEntity resultingEntity = null!;
        var id = Guid.Parse("bee5fc57-4615-4f53-a168-f7acf64824df");
        var existingEntity = new FacilityEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 07, 18, 14, 41, 11),
            SiteId = "D54291",
            Name = "D54291",
            Type = FacilityType.Lf,
            LegalEntity = "maec1",
            AdminEmail = "some-email!",
            SourceLocation = "test-loc!",
            Id = id,
            Pipeline = "",
            Treating = "",
            Terminaling = "",
            Waste = "",
            Water = "",
        };

        var expectedEntity = new FacilityEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 07, 19, 18, 08, 35),
            SiteId = "D54292",
            Name = "D54292",
            Type = FacilityType.Fst,
            LegalEntity = "maec",
            AdminEmail = "new-email",
            SourceLocation = "test-loc",
            Id = id,
            Pipeline = "",
            Treating = "",
            Terminaling = "",
            Waste = "",
            Water = "",
        };

        var message = typeof(FacilityEntityProcessorTests).Assembly.GetResourceAsString("EntityFunctions-Facility-Sample.json", "Resources")!;
        _manager.Setup(m => m.Save(It.IsAny<FacilityEntity>(), It.IsAny<bool>()))!.Callback((FacilityEntity c, bool d) => resultingEntity = c);
        _manager.Setup(m => m.GetById(It.Is<Guid>(g => g == id), It.IsAny<bool>()))!.Returns(Task.FromResult(existingEntity));

        // act
        await _entityProcessor.Process(message);

        // assert
        resultingEntity.SiteId.Should().BeEquivalentTo(expectedEntity.SiteId);
        resultingEntity.Name.Should().BeEquivalentTo(expectedEntity.Name);
        resultingEntity.LegalEntityId.Should().Be(expectedEntity.LegalEntityId);
        resultingEntity.AdminEmail.Should().BeEquivalentTo(expectedEntity.AdminEmail);
    }

    [TestMethod("FacilityEntityProcessor should be able to process a normal message for a new entity.")]
    public async Task FacilityEntityProcessor_Process_NormalMessageNew()
    {
        // arrange
        FacilityEntity resultingEntity = null!;
        var id = Guid.Parse("bee5fc57-4615-4f53-a168-f7acf64824df");
        var expectedEntity = new FacilityEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 07, 19, 18, 08, 35),
            SiteId = "D54292",
            Name = "D54292",
            Type = FacilityType.Fst,
            LegalEntity = "maec",
            AdminEmail = "new-email",
            SourceLocation = "test-loc",
            Id = id,
            Pipeline = "",
            Treating = "",
            Terminaling = "",
            Waste = "",
            Water = "",
        };

        var message = typeof(FacilityEntityProcessorTests).Assembly.GetResourceAsString("EntityFunctions-Facility-Sample.json", "Resources")!;
        _manager.Setup(m => m.Save(It.IsAny<FacilityEntity>(), It.IsAny<bool>()))!.Callback((FacilityEntity c, bool d) => resultingEntity = c);
        _manager.Setup(m => m.GetById(It.Is<Guid>(g => g == id), It.IsAny<bool>()))!.Returns(Task.FromResult((FacilityEntity)null));

        // act
        await _entityProcessor.Process(message);

        // assert
        resultingEntity.SiteId.Should().BeEquivalentTo(expectedEntity.SiteId);
        resultingEntity.Name.Should().BeEquivalentTo(expectedEntity.Name);
        resultingEntity.LegalEntityId.Should().Be(expectedEntity.LegalEntityId);
        resultingEntity.AdminEmail.Should().BeEquivalentTo(expectedEntity.AdminEmail);
    }

    [TestMethod("FacilityEntityProcessor should be able to skip outdated messages.")]
    public async Task FacilityEntityProcessor_Process_SkipOutdated()
    {
        // arrange
        var id = Guid.Parse("bee5fc57-4615-4f53-a168-f7acf64824df");
        var existingEntity = new FacilityEntity
        {
            LastIntegrationDateTime = new DateTime(2022, 07, 21, 14, 41, 11),
            SiteId = "D54292",
            Name = "D54292",
            Type = FacilityType.Fst,
            LegalEntity = "maec",
            AdminEmail = "some-email",
            SourceLocation = "test-loc",
            Id = id,
            Pipeline = "",
            Treating = "",
            Terminaling = "",
            Waste = "",
            Water = "",
        };

        var message = typeof(FacilityEntityProcessorTests).Assembly.GetResourceAsString("EntityFunctions-Facility-Sample.json", "Resources")!;
        _manager.Setup(m => m.GetById(It.Is<Guid>(g => g == id), It.IsAny<bool>()))!.Returns(Task.FromResult(existingEntity));

        // act
        await _entityProcessor.Process(message);

        // assert
        _log.Verify(l => l.Warning(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Message is outdated"))!, It.IsAny<object[]>()!));
    }
}
