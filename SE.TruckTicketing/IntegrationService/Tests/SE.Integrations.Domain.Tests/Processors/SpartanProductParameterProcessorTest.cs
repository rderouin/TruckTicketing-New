using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;
using SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class SpartanProductParameterProcessorTest
{
    private Mock<IManager<Guid, SpartanProductParameterEntity>> _manager = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    private SpartanProductParameterProcessor _processor = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();

        _processor = new SpartanProductParameterProcessor(_serviceMapperRegistry, _manager.Object!);
    }

    [TestMethod]
    public async Task SpartanProductParameter_Process_DataMigration_MessageProcessed()
    {
        //arrange
        var spartanProductParameter = GenFu.GenFu.New<SpartanProductParameter>();
        var entityEnvelop = new EntityEnvelopeModel<SpartanProductParameter>();
        entityEnvelop.Payload = spartanProductParameter;

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
