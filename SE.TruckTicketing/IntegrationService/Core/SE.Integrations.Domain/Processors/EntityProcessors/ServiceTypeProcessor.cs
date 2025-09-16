using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.ServiceType;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("ServiceType")]
public class ServiceTypeProcessor : BaseEntityProcessor<ServiceType>
{
    private readonly IManager<Guid, ServiceTypeEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public ServiceTypeProcessor(IMapperRegistry mapperRegistry,
                                IManager<Guid, ServiceTypeEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<ServiceType> entityModel)
    {
        var newEntity = _mapperRegistry.Map<ServiceTypeEntity>(entityModel.Payload);

        await _manager.Save(newEntity);
    }
}
