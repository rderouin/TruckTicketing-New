using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.BusinessStream;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.BusinessStream)]
public class BusinessStreamProcessor : BaseEntityProcessor<BusinessStreamEntity>
{
    private readonly IManager<Guid, BusinessStreamEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public BusinessStreamProcessor(IMapperRegistry mapperRegistry,
                                   IManager<Guid, BusinessStreamEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<BusinessStreamEntity> entityModel)
    {
        var newEntity = _mapperRegistry.Map<BusinessStreamEntity>(entityModel.Payload);
        await _manager.Save(newEntity);
    }
}
