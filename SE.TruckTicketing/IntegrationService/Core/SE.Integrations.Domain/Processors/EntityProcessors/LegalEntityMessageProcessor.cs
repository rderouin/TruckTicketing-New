using System;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.BusinessStream;
using SE.Shared.Domain.Extensions;
using SE.Shared.Domain.LegalEntity;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Contracts.Configuration;
using Trident.Data.Contracts;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.LegalEntity)]
public class LegalEntityMessageProcessor : BaseEntityProcessor<LegalEntityModel>
{
    private readonly IAppSettings _appSettings;

    private readonly IProvider<Guid, BusinessStreamEntity> _businessStreamProvider;

    private readonly IManager<Guid, LegalEntityEntity> _legalEntityManager;

    public LegalEntityMessageProcessor(IManager<Guid, LegalEntityEntity> legalEntityManager, IProvider<Guid, BusinessStreamEntity> businessStreamProvider, IAppSettings appSettings)
    {
        _legalEntityManager = legalEntityManager;
        _businessStreamProvider = businessStreamProvider;
        _appSettings = appSettings;
    }

    public override async Task Process(EntityEnvelopeModel<LegalEntityModel> message)
    {
        LegalEntityEntity legalEntity = new();
        //Capture existing LegalEntity if exist
        var existingLegalEntity = await _legalEntityManager.GetById(message.EnterpriseId);

        //Enrich BusinessStream for incoming LegalEntity
        await EnrichBusinessStreamInfo(message.Payload, legalEntity);

        //Read app settings to capture Business Stream for which PrimaryContactRequired will be enforced for all the Legal Entities
        var isCustomerPrimaryContactRequired =
            !legalEntity.BusinessStreamId.HasValue || legalEntity.BusinessStreamId == default ? null : await PrimaryContactRequiredByBusinessStreamCheck(legalEntity);

        //Hydrate LegalEntity entity with information received from incoming LegalEntity model
        legalEntity = MapLegalEntity(message.Payload, existingLegalEntity ?? legalEntity, isCustomerPrimaryContactRequired);
        legalEntity.Id = message.EnterpriseId;

        await _legalEntityManager.Save(legalEntity);
    }

    private async Task<bool?> PrimaryContactRequiredByBusinessStreamCheck(LegalEntityEntity entity)
    {
        var enforceCustomerContactRequiredByBusinessStream = _appSettings.LegalEntityCustomerPrimaryContactConstraintCheckerExtension();

        if (enforceCustomerContactRequiredByBusinessStream == null || !enforceCustomerContactRequiredByBusinessStream.IgnorePrimaryContactRequiredByBusinessSteam.Any())
        {
            return null;
        }

        foreach (var businessStream in enforceCustomerContactRequiredByBusinessStream.IgnorePrimaryContactRequiredByBusinessSteam)
        {
            var businessStreamEntitiesToEnforceCustomerContactConstraint = await _businessStreamProvider.Get(x => x.Name == businessStream);
            var businessStreamEntities = businessStreamEntitiesToEnforceCustomerContactConstraint?.Select(x => x.Id).ToList();

            if (businessStreamEntitiesToEnforceCustomerContactConstraint == null || !businessStreamEntities.Any())
            {
                continue;
            }

            if (entity.BusinessStreamId != null && !businessStreamEntities.Contains(entity.BusinessStreamId.Value))
            {
                continue;
            }

            return true;
        }

        return null;
    }

    private LegalEntityEntity MapLegalEntity(LegalEntityModel legalEntity, LegalEntityEntity entity, bool? enforceCustomerContactRequired)
    {
        entity.CountryCode = legalEntity.CountryCode;
        entity.Code = legalEntity.Code.ToUpper();
        entity.Name = legalEntity.Name.ToUpper();
        entity.CreditExpirationThreshold = legalEntity.CreditExpirationThreshold;
        entity.IsCustomerPrimaryContactRequired = enforceCustomerContactRequired ?? (legalEntity.IsCustomerPrimaryContactRequired ?? false);
        entity.ShowAccountsInTruckTicketing = enforceCustomerContactRequired ?? legalEntity.ShowCustomersInTruckTicking;
        entity.RemitToDuns = legalEntity.RemitToDuns;
        return entity;
    }

    private async Task EnrichBusinessStreamInfo(LegalEntityModel model, LegalEntityEntity entity)
    {
        var businessStreams = await _businessStreamProvider.Get(stream => stream.Name == model.BusinessStream);

        var businessStreamEntities = businessStreams?.ToList();
        if (businessStreams == null || !businessStreamEntities.Any() || businessStreamEntities.FirstOrDefault()?.Id == Guid.Empty)
        {
            var newBusinessStreamEntity = new BusinessStreamEntity
            {
                Name = model.BusinessStream,
                Id = Guid.NewGuid(),
            };

            entity.BusinessStreamId = newBusinessStreamEntity.Id;
            await _businessStreamProvider.Insert(newBusinessStreamEntity);
        }
        else
        {
            var businessStreamId = businessStreamEntities.FirstOrDefault()?.Id ?? Guid.Empty;
            entity.BusinessStreamId = businessStreamId;
        }
    }
}

public class LegalEntityModel
{
    public string BusinessStream { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }

    public CountryCode CountryCode { get; set; }

    [JsonProperty("CreditExpiryThreshold")]
    public int CreditExpirationThreshold { get; set; }

    public bool? IsCustomerPrimaryContactRequired { get; set; }

    public bool? ShowCustomersInTruckTicking { get; set; }
    
    public string RemitToDuns { get; set; }
}
