using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class BillingConfigurationProcessorTest
{
    private Mock<IManager<Guid, BillingConfigurationEntity>> _manager = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    private BillingConfigurationProcessor _processor = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();

        _processor = new BillingConfigurationProcessor(_serviceMapperRegistry, _manager.Object!);
    }

    [TestMethod]
    public async Task BillingConfiguration_Process_AcceptedMessage()
    {
        //arrange
        var bc = GenFu.GenFu.New<BillingConfiguration>();
        var entityEnvelop = new EntityEnvelopeModel<BillingConfiguration>();
        entityEnvelop.Payload = bc;

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
