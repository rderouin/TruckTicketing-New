using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("SourceLocation")]
public class SourceLocationProcessor : BaseEntityProcessor<SourceLocation>
{
    private readonly IManager<Guid, SourceLocationEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public SourceLocationProcessor(IMapperRegistry mapperRegistry,
                                   IManager<Guid, SourceLocationEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<SourceLocation> entityModel)
    {
        var newEntity = _mapperRegistry.Map<SourceLocationEntity>(entityModel.Payload);
        newEntity = UpdateAuditFields(newEntity, "DataMigration");
        await _manager.Save(newEntity);
    }

    private SourceLocationEntity UpdateAuditFields(SourceLocationEntity model, string user)
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
