using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class InvoiceConfigurationProcessorTest
{
    private Mock<IManager<Guid, InvoiceConfigurationEntity>> _manager = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    private InvoiceConfigurationProcessor _processor = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();

        _processor = new InvoiceConfigurationProcessor(_serviceMapperRegistry, _manager.Object!);
    }

    [TestMethod]
    public async Task InvoiceConfiguration_Process_DataMigration_MessageProcessed()
    {
        //arrange
        var invoiceConfiguration = GenFu.GenFu.New<InvoiceConfiguration>();
        var entityEnvelop = new EntityEnvelopeModel<InvoiceConfiguration>();
        entityEnvelop.Payload = invoiceConfiguration;

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
