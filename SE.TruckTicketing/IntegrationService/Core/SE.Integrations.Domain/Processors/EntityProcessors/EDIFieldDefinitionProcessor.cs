using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.EDIFieldDefinition;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("EDIFieldDefinition")]
public class EDIFieldDefinitionProcessor : BaseEntityProcessor<EDIFieldDefinitionEntity>
{
    private readonly IManager<Guid, EDIFieldDefinitionEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public EDIFieldDefinitionProcessor(IMapperRegistry mapperRegistry,
                                       IManager<Guid, EDIFieldDefinitionEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<EDIFieldDefinitionEntity> entityModel)
    {
        var newEntity = _mapperRegistry.Map<EDIFieldDefinitionEntity>(entityModel.Payload);
        newEntity = UpdateAuditFields(newEntity, "DataMigration");
        await _manager.Save(newEntity);
    }

    private EDIFieldDefinitionEntity UpdateAuditFields(EDIFieldDefinitionEntity model, string user)
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
