using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("MaterialApproval")]
public class MaterialApprovalProcessor : BaseEntityProcessor<MaterialApproval>
{
    private readonly IManager<Guid, MaterialApprovalEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public MaterialApprovalProcessor(IMapperRegistry mapperRegistry,
                                     IManager<Guid, MaterialApprovalEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<MaterialApproval> entityModel)
    {
        var newEntity = _mapperRegistry.Map<MaterialApprovalEntity>(entityModel.Payload);
        newEntity = UpdateAuditFields(newEntity, "DataMigration");
        await _manager.Save(newEntity);
    }

    private MaterialApprovalEntity UpdateAuditFields(MaterialApprovalEntity model, string user)
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
