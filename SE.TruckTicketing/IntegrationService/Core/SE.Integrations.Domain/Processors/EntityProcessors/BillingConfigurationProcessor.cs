using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("BillingConfiguration")]
public class BillingConfigurationProcessor : BaseEntityProcessor<BillingConfiguration>
{
    private readonly IManager<Guid, BillingConfigurationEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public BillingConfigurationProcessor(IMapperRegistry mapperRegistry,
                                         IManager<Guid, BillingConfigurationEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<BillingConfiguration> entityModel)
    {
        var newEntity = _mapperRegistry.Map<BillingConfigurationEntity>(entityModel.Payload);
        newEntity = UpdateAuditFields(newEntity, "DataMigration");
        await _manager.Save(newEntity);
    }

    private BillingConfigurationEntity UpdateAuditFields(BillingConfigurationEntity model, string user)
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
