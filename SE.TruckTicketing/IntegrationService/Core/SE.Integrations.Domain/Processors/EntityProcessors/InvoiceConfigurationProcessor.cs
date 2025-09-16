using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("InvoiceConfiguration")]
public class InvoiceConfigurationProcessor : BaseEntityProcessor<InvoiceConfiguration>
{
    private readonly IManager<Guid, InvoiceConfigurationEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public InvoiceConfigurationProcessor(IMapperRegistry mapperRegistry,
                                         IManager<Guid, InvoiceConfigurationEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<InvoiceConfiguration> entityModel)
    {
        var newEntity = _mapperRegistry.Map<InvoiceConfigurationEntity>(entityModel.Payload);
        newEntity = UpdateAuditFields(newEntity, "DataMigration");
        await _manager.Save(newEntity);
    }

    private InvoiceConfigurationEntity UpdateAuditFields(InvoiceConfigurationEntity model, string user)
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
