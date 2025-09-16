using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.BusinessStream;
using SE.Shared.Domain.Extensions;
using SE.Shared.Domain.LegalEntity;

using Trident.Contracts;
using Trident.Contracts.Configuration;
using Trident.Data.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.LegalEntityMessage)]
public class LegalEntityProcessor : BaseEntityProcessor<LegalEntityEntity>
{
    private readonly IAppSettings _appSettings;

    private readonly IProvider<Guid, BusinessStreamEntity> _businessStreamProvider;

    private readonly IManager<Guid, LegalEntityEntity> _legalEntityManager;

    private readonly IMapperRegistry _mapperRegistry;

    public LegalEntityProcessor(IManager<Guid, LegalEntityEntity> legalEntityManager,
                                IMapperRegistry mapperRegistry,
                                IProvider<Guid, BusinessStreamEntity> businessStreamProvider,
                                IAppSettings appSettings)
    {
        _legalEntityManager = legalEntityManager;
        _mapperRegistry = mapperRegistry;
        _businessStreamProvider = businessStreamProvider;
        _appSettings = appSettings;
    }

    public override async Task Process(EntityEnvelopeModel<LegalEntityEntity> entityModel)
    {
        var legalEntity = _mapperRegistry.Map<LegalEntityEntity>(entityModel.Payload);
        //Read app settings to capture Business Stream for which PrimaryContactRequired will be enforced for all the Legal Entities
        var enforceCustomerContactRequired =
            !legalEntity.BusinessStreamId.HasValue || legalEntity.BusinessStreamId == default ? null : await PrimaryContactRequiredByBusinessStreamCheck(legalEntity);

        if (enforceCustomerContactRequired.HasValue && enforceCustomerContactRequired.Value)
        {
            legalEntity.IsCustomerPrimaryContactRequired = true;
            legalEntity.ShowAccountsInTruckTicketing = true;
        }

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
}
