using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.SourceLocation;

using Trident.Contracts;
using Trident.Mapper;

using SE.TruckTicketing.Contracts.Models.SourceLocations;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class SourceLocationProcessorTest
{
    private Mock<IManager<Guid, SourceLocationEntity>> _manager = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    private SourceLocationProcessor _processor = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();

        _processor = new SourceLocationProcessor(_serviceMapperRegistry, _manager.Object!);
    }

    [TestMethod]
    public async Task SourceLocation_Process_DataMigration_MessageProcessed()
    {
        //arrange
        var sourceLocation = GenFu.GenFu.New<SourceLocation>();
        var entityEnvelop = new EntityEnvelopeModel<SourceLocation>();
        entityEnvelop.Payload = sourceLocation;

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
