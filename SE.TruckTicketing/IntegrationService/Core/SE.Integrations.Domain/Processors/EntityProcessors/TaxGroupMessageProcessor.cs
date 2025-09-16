using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.TaxGroups;
using SE.Shared.Domain.LegalEntity;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.TaxGroup)]
public class TaxGroupMessageProcessor : BaseEntityProcessor<TaxGroupMessage>
{
    private readonly IProvider<Guid, LegalEntityEntity> _legalEntityProvider;

    private readonly ILog _log;

    private readonly IManager<Guid, TaxGroupEntity> _taxGroupManager;

    public TaxGroupMessageProcessor(IManager<Guid, TaxGroupEntity> taxGroupManager,
                                    IProvider<Guid, LegalEntityEntity> legalEntityProvider,
                                    ILog log)
    {
        _taxGroupManager = taxGroupManager;
        _legalEntityProvider = legalEntityProvider;
        _log = log;
    }

    public override async Task Process(EntityEnvelopeModel<TaxGroupMessage> entityModel)
    {
        var taxGroupEntity = await MapToEntity(entityModel.Payload);
        // fetch existing if there is one
        var existingEntity = await _taxGroupManager.GetById(taxGroupEntity.Id)!;
        if (existingEntity?.LastIntegrationDateTime > entityModel.MessageDate)
        {
            _log.Warning(messageTemplate: $"Message is outdated. (CorrelationId: {entityModel.CorrelationId})");
            return;
        }

        // copy only required fields if existing, otherwise create a new one
        if (existingEntity == null)
        {
            existingEntity = taxGroupEntity;
        }
        else
        {
            CopyOnlyRequiredFields(taxGroupEntity, existingEntity);
        }

        existingEntity.LastIntegrationDateTime = entityModel.MessageDate;
        existingEntity = UpdateAuditFields(existingEntity, "Integrations");
        await _taxGroupManager.Save(existingEntity);
    }

    private async Task<TaxGroupEntity> MapToEntity(TaxGroupMessage taxGroup)
    {
        var legalEntity = (await _legalEntityProvider.Get(legalEntity => legalEntity.Code.ToLower() == taxGroup.DataAreaId.ToLower())).FirstOrDefault();

        if (legalEntity is null)
        {
            throw new InvalidOperationException($"Legal entity ({taxGroup.DataAreaId}) does not exist");
        }

        var entity = new TaxGroupEntity
        {
            Id = taxGroup.Id,
            LegalEntityName = taxGroup.DataAreaId.ToUpper(),
            LegalEntityId = legalEntity.Id,
            TaxCodes = taxGroup.TaxCodes.Select(MapToEntity).ToList(),
            Name = taxGroup.TaxGroupName,
            Group = taxGroup.TaxGroup,
        };

        return entity;
    }

    private TaxCodeEntity MapToEntity(TaxCodeDto taxCode)
    {
        return new()
        {
            TaxName = taxCode.TaxName,
            TaxValuePercentage = taxCode.TaxValue,
            Code = taxCode.TaxCode,
            ExemptTax = taxCode.ExemptTax,
            CurrencyCode = taxCode.CurrencyCode,
            UseTax = taxCode.UseTax,
        };
    }

    private TaxGroupEntity UpdateAuditFields(TaxGroupEntity model, string user)
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

    private void CopyOnlyRequiredFields(TaxGroupEntity sourceEntity, TaxGroupEntity destinationEntity)
    {
        // NOTE: copy only fields present in SB payload
        destinationEntity.Name = sourceEntity.Name;
        destinationEntity.LegalEntityId = sourceEntity.LegalEntityId;
        destinationEntity.LegalEntityName = sourceEntity.LegalEntityName;
        destinationEntity.TaxCodes = sourceEntity.TaxCodes;
        destinationEntity.Group = sourceEntity.Group;
    }
}

public class TaxGroupMessage
{
    public Guid Id { get; set; }

    public string TaxGroup { get; set; }

    public string TaxGroupName { get; set; }

    public string DataAreaId { get; set; }

    public List<TaxCodeDto> TaxCodes { get; set; }
}

public class TaxCodeDto
{
    public string TaxCode { get; set; }

    public string TaxName { get; set; }

    public string CurrencyCode { get; set; }

    public bool ExemptTax { get; set; }

    public bool UseTax { get; set; }

    public double TaxValue { get; set; }
}
