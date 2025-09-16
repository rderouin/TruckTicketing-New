using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class SourceLocationTypeProcessorTest
{
    private Mock<IManager<Guid, SourceLocationTypeEntity>> _manager = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    private SourceLocationTypeProcessor _processor = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();

        _processor = new SourceLocationTypeProcessor(_serviceMapperRegistry, _manager.Object!);
    }

    [TestMethod]
    public async Task SourceLocationType_Process_DataMigration_MessageProcessed()
    {
        //arrange
        var sourceLocationType = GenFu.GenFu.New<SourceLocationType>();
        var entityEnvelop = new EntityEnvelopeModel<SourceLocationType>();
        entityEnvelop.Payload = sourceLocationType;

        Exception exc = null;

        //act
        try
        {
            await _processor.Process(entityEnvelop);
        }
        catch (Exception ex)
        {
            exc = ex;
        }

        //assert
        exc.Should().BeNull();
    }
}
