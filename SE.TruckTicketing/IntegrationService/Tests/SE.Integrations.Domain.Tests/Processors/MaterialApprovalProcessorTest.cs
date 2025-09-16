using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Configuration;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class MaterialApprovalProcessorTest
{
    private Mock<IManager<Guid, MaterialApprovalEntity>> _manager = null!;

    private ServiceMapperRegistry _serviceMapperRegistry = null!;

    private MaterialApprovalProcessor _processor = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMapperRegistry = new(new[] { new ApiMapperProfile() });
        _manager = new();

        _processor = new MaterialApprovalProcessor(_serviceMapperRegistry, _manager.Object!);
    }

    [TestMethod]
    public async Task FacilityService_Process_DataMigration_MessageProcessed()
    {
        //arrange
        var materialApproval = GenFu.GenFu.New<MaterialApproval>();
        var entityEnvelop = new EntityEnvelopeModel<MaterialApproval>();
        entityEnvelop.Payload = materialApproval;

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
