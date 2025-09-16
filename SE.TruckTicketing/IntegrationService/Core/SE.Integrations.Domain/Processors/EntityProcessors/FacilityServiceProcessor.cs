using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Domain.Entities.FacilityService;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("FacilityService")]
public class FacilityServiceProcessor : BaseEntityProcessor<FacilityService>
{
    private readonly IManager<Guid, FacilityServiceEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public FacilityServiceProcessor(IMapperRegistry mapperRegistry,
                                    IManager<Guid, FacilityServiceEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<FacilityService> entityModel)
    {
        var newEntity = _mapperRegistry.Map<FacilityServiceEntity>(entityModel.Payload);

        await _manager.Save(newEntity);
    }
}
