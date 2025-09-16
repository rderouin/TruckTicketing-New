using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;
using SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("SpartanProductParameter")]
public class SpartanProductParameterProcessor : BaseEntityProcessor<SpartanProductParameter>
{
    private readonly IManager<Guid, SpartanProductParameterEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public SpartanProductParameterProcessor(IMapperRegistry mapperRegistry,
                                            IManager<Guid, SpartanProductParameterEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<SpartanProductParameter> entityModel)
    {
        var newEntity = _mapperRegistry.Map<SpartanProductParameterEntity>(entityModel.Payload);
        newEntity = UpdateAuditFields(newEntity, "DataMigration");
        await _manager.Save(newEntity);
    }

    private SpartanProductParameterEntity UpdateAuditFields(SpartanProductParameterEntity model, string user)
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
