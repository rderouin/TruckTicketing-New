using System;
using System.Threading.Tasks;

using Moq;

using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.Integrations.Api.Configuration;

using Trident.Contracts;
using Trident.Mapper;

using FluentAssertions;

using SE.Enterprise.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.FacilityServices;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class FacilityServiceProcessorTest
{
    private Mock<IManager<Guid, FacilityServiceEntity>> _manager = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    private FacilityServiceProcessor _processor = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();

        _processor = new FacilityServiceProcessor(_serviceMapperRegistry, _manager.Object!);
    }

    [TestMethod]
    public async Task FacilityService_Process_DataMigration_MessageProcessed()
    {
        //arrange
        var facilityService = GenFu.GenFu.New<FacilityService>();
        var entityEnvelop = new EntityEnvelopeModel<FacilityService>();
        entityEnvelop.Payload = facilityService;

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
