using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.ServiceType;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class ServiceTypeProcessorTest
{
    private Mock<IManager<Guid, ServiceTypeEntity>> _manager = null!;

    private ServiceTypeProcessor _processor;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();

        _processor = new(_serviceMapperRegistry, _manager.Object!);
    }

    [TestMethod]
    public async Task ServiceType_Process_DataMigration_MessageProcessed()
    {
        //arrange
        var serviceType = GenFu.GenFu.New<ServiceType>();
        var entityEnvelop = new EntityEnvelopeModel<ServiceType>();
        entityEnvelop.Payload = serviceType;

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
