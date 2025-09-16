using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("SourceLocationType")]
public class SourceLocationTypeProcessor : BaseEntityProcessor<SourceLocationType>
{
    private readonly IManager<Guid, SourceLocationTypeEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public SourceLocationTypeProcessor(IMapperRegistry mapperRegistry,
                                       IManager<Guid, SourceLocationTypeEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<SourceLocationType> entityModel)
    {
        var newEntity = _mapperRegistry.Map<SourceLocationTypeEntity>(entityModel.Payload);
        newEntity = UpdateAuditFields(newEntity, "DataMigration");
        await _manager.Save(newEntity);
    }

    private SourceLocationTypeEntity UpdateAuditFields(SourceLocationTypeEntity model, string user)
    {
        if (!model.CreatedBy.HasText())
        {
            model.CreatedAt = DateTimeOffset.UtcNow;
            model.CreatedBy = user;
        }

        model.UpdatedAt = DateTimeOffset.UtcNow;
        model.UpdatedBy = user;
        return model;
    }
}
