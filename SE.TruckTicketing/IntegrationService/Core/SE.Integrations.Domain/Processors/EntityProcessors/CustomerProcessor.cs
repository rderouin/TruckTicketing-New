using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.LegalEntity;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.Customer)]
public class CustomerProcessor : BaseEntityProcessor<CustomerModel>
{
    private readonly IProvider<Guid, LegalEntityEntity> _legalEntityProvider;

    private readonly ILog _log;

    private readonly IManager<Guid, AccountEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public CustomerProcessor(IMapperRegistry mapperRegistry,
                             IManager<Guid, AccountEntity> manager,
                             ILog log,
                             IProvider<Guid, LegalEntityEntity> legalEntityProvider)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
        _log = log;
        _legalEntityProvider = legalEntityProvider;
    }

    public override async Task Process(EntityEnvelopeModel<CustomerModel> customerModel)
    {
        var newEntity = _mapperRegistry.Map<AccountEntity>(customerModel.Payload)!;
        newEntity.Attachments = customerModel.Blobs?.Select(x => _mapperRegistry.Map<AccountAttachmentEntity>(x)).ToList();
        await EnrichLegalEntityInfo(customerModel.Payload, newEntity);

        // fetch existing if there is one
        var existingEntity = await _manager.GetById(newEntity.Id)!;
        if (existingEntity?.LastIntegrationDateTime > customerModel.MessageDate)
        {
            _log.Warning(messageTemplate: $"Message is outdated. (CorrelationId: {customerModel.CorrelationId})");
            return;
        }

        // copy only required fields if existing, otherwise create a new one
        if (existingEntity == null)
        {
            existingEntity = newEntity;
            existingEntity.AccountTypes.List.Add(AccountTypes.Customer.ToString());
        }
        else
        {
            CopyOnlyRequiredFields(newEntity, existingEntity);
        }

        existingEntity.LastIntegrationDateTime = customerModel.MessageDate;
        existingEntity = UpdateAuditFields(existingEntity, "Integrations");
        // save to db
        await _manager.Save(existingEntity)!;
    }

    private async Task EnrichLegalEntityInfo(CustomerModel source, AccountEntity accountEntity)
    {
        var legalEntity = (await _legalEntityProvider.Get(p => p.Code.ToLower() == source.DataAreaId.ToLower()))?.FirstOrDefault();
        accountEntity.LegalEntityId = legalEntity?.Id ?? Guid.Empty;
        accountEntity.LegalEntity = legalEntity?.Code?.ToUpper();
    }

    private void CopyOnlyRequiredFields(AccountEntity sourceEntity, AccountEntity destinationEntity)
    {
        // NOTE: copy only fields present in SB payload
        destinationEntity.Name = sourceEntity.Name;
        destinationEntity.LegalEntityId = sourceEntity.LegalEntityId;
        destinationEntity.IsBlocked = sourceEntity.IsBlocked;
        destinationEntity.Email = sourceEntity.Email;
        destinationEntity.Attachments = sourceEntity.Attachments;
        destinationEntity.EnableCreditMessagingRedFlag = sourceEntity.EnableCreditMessagingRedFlag;
        destinationEntity.AccountAddresses = sourceEntity.AccountAddresses;
        destinationEntity.EnableCreditMessagingRedFlag = sourceEntity.EnableCreditMessagingRedFlag;
        destinationEntity.WatchListStatus = sourceEntity.WatchListStatus;
        destinationEntity.AccountStatus = sourceEntity.AccountStatus;
        if (!destinationEntity.AccountTypes.List.Contains(AccountTypes.Customer.ToString()))
        {
            destinationEntity.AccountTypes.List.Add(AccountTypes.Customer.ToString());
        }

        destinationEntity.BillingTransferRecipientId = sourceEntity.BillingTransferRecipientId;
        destinationEntity.BillingTransferRecipientName = sourceEntity.BillingTransferRecipientName;
        destinationEntity.BillingType = sourceEntity.BillingType;
        destinationEntity.CustomerNumber = sourceEntity.CustomerNumber;
        destinationEntity.CreditLimit = sourceEntity.CreditLimit;
        destinationEntity.CreditStatus = sourceEntity.CreditStatus;
        destinationEntity.DUNSNumber = sourceEntity.DUNSNumber;
        destinationEntity.GSTNumber = sourceEntity.GSTNumber;
        destinationEntity.HasPriceBook = sourceEntity.HasPriceBook;
        destinationEntity.IsEdiFieldsEnabled = sourceEntity.IsEdiFieldsEnabled;
        destinationEntity.IsElectronicBillingEnabled = sourceEntity.IsElectronicBillingEnabled;
        destinationEntity.OperatorLicenseCode = sourceEntity.OperatorLicenseCode;
        destinationEntity.NetOff = sourceEntity.NetOff;
        destinationEntity.CreditApplicationReceived = sourceEntity.CreditApplicationReceived;
        destinationEntity.EnableCreditMessagingGeneral = sourceEntity.EnableCreditMessagingGeneral;
        destinationEntity.PriceGroup = sourceEntity.PriceGroup;
        destinationEntity.TmaGroup = sourceEntity.TmaGroup;
    }

    private AccountEntity UpdateAuditFields(AccountEntity model, string user)
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
