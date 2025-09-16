using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.LegalEntity;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.Facility)]
public class FacilityEntityProcessor : BaseEntityProcessor<FacilityModel>
{
    private readonly IProvider<Guid, LegalEntityEntity> _legalEntityProvider;

    private readonly ILog _log;

    private readonly IManager<Guid, FacilityEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public FacilityEntityProcessor(IMapperRegistry mapperRegistry, IManager<Guid, FacilityEntity> manager, ILog log, IProvider<Guid, LegalEntityEntity> legalEntityProvider)
    {
        _mapperRegistry = mapperRegistry;
        _legalEntityProvider = legalEntityProvider;
        _manager = manager;
        _log = log;
    }

    public override async Task Process(EntityEnvelopeModel<FacilityModel> entityModel)
    {
        // convert into an entity
        var newEntity = _mapperRegistry.Map<FacilityEntity>(entityModel.Payload)!;
        await EnrichLegalEntityInfo(entityModel.Payload, newEntity);
        // fetch existing if there is one
        var existingEntity = await _manager.GetById(newEntity.Id)!;
        if (existingEntity?.LastIntegrationDateTime > entityModel.MessageDate)
        {
            _log.Warning(messageTemplate: $"Message is outdated. (CorrelationId: {entityModel.CorrelationId})");
            return;
        }

        // copy only required fields if existing, otherwise create a new one
        if (existingEntity == null)
        {
            existingEntity = newEntity;
        }
        else
        {
            CopyOnlyRequiredFields(newEntity, existingEntity);
        }

        existingEntity.LastIntegrationDateTime = entityModel.MessageDate;
        // save to db
        await _manager.Save(existingEntity)!;
    }

    private async Task EnrichLegalEntityInfo(FacilityModel source, FacilityEntity facilityEntity)
    {
        var legalEntity = (await _legalEntityProvider.Get(p => p.Code.ToLower() == source.LegalEntity.ToLower()))?.FirstOrDefault();
        facilityEntity.CountryCode = legalEntity?.CountryCode ?? CountryCode.Undefined;
        facilityEntity.LegalEntityId = legalEntity?.Id ?? Guid.Empty;
    }

    private void CopyOnlyRequiredFields(FacilityEntity sourceEntity, FacilityEntity destinationEntity)
    {
        // NOTE: copy only the fields that present in the SB payload
        destinationEntity.Type = sourceEntity.Type;
        destinationEntity.AdminEmail = sourceEntity.AdminEmail;
        destinationEntity.SiteId = sourceEntity.SiteId;
        destinationEntity.Name = sourceEntity.Name;
        destinationEntity.LegalEntity = sourceEntity.LegalEntity.ToUpper();
        destinationEntity.SourceLocation = sourceEntity.SourceLocation;
        destinationEntity.CountryCode = sourceEntity.CountryCode;
        destinationEntity.Province = sourceEntity.Province;
        destinationEntity.Waste = sourceEntity.Waste;
        destinationEntity.Water = sourceEntity.Water;
        destinationEntity.Pipeline = sourceEntity.Pipeline;
        destinationEntity.Terminaling = sourceEntity.Terminaling;
        destinationEntity.Treating = sourceEntity.Treating;
        destinationEntity.IsActive = sourceEntity.IsActive;
    }
}
