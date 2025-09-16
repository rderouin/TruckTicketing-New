using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.AccountContact)]
public class AccountContactIndexProcessor : BaseEntityProcessor<AccountContactModel>
{
    private readonly ILog _log;

    private readonly IManager<Guid, AccountEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public AccountContactIndexProcessor(IMapperRegistry mapperRegistry, IManager<Guid, AccountEntity> manager, ILog log)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
        _log = log;
    }

    public override async Task Process(EntityEnvelopeModel<AccountContactModel> accountContactModel)
    {
        var incomingAccountContactEntity = _mapperRegistry.Map<AccountContactEntity>(accountContactModel.Payload)!;

        var existingAccountEntity = await _manager.GetById(accountContactModel.Payload.AccountId)!;

        if (existingAccountEntity == null)
        {
            _log.Error(messageTemplate: $"Account cannot be found. (CorrelationId: {accountContactModel.CorrelationId}, AccountID :{accountContactModel.Payload.AccountId})");
        }
        else
        {
            var existingAccountContactEntity = existingAccountEntity.Contacts?.FirstOrDefault(x => x.Id == accountContactModel.EnterpriseId);
            var accountContactEntity = CopyOnlyRequiredFields(accountContactModel.Payload, existingAccountContactEntity ?? new());

            if (existingAccountContactEntity == null)
            {
                if (existingAccountEntity.Contacts == null)
                {
                    existingAccountEntity.Contacts = new();
                }

                accountContactEntity.Id = accountContactModel.EnterpriseId;
                existingAccountEntity.Contacts.Add(accountContactEntity);
            }

            // save to db
            await _manager.Save(existingAccountEntity)!;
        }
    }

    private AccountContactEntity CopyOnlyRequiredFields(AccountContactModel sourceEntity, AccountContactEntity destinationEntity)
    {
        // NOTE: copy only fields present in SB payload

        destinationEntity.Name = sourceEntity.FirstName;
        destinationEntity.IsPrimaryAccountContact = sourceEntity.IsPrimaryAccountContact;
        destinationEntity.LastName = sourceEntity.LastName;
        destinationEntity.Email = sourceEntity.Email;
        destinationEntity.PhoneNumber = sourceEntity.PhoneNumber.Length == 10 ? Regex.Replace(sourceEntity.PhoneNumber, @"(\d{3})(\d{3})(\d{4})", "($1)-$2-$3") : sourceEntity.PhoneNumber;
        destinationEntity.JobTitle = sourceEntity.JobTitle;
        destinationEntity.Contact = sourceEntity.Contact;
        destinationEntity.IsActive = sourceEntity.IsActive;
        if (!destinationEntity.ContactFunctions.List.Contains(AccountContactFunctions.BillingContact.ToString()))
        {
            destinationEntity.ContactFunctions.List.Add(AccountContactFunctions.BillingContact.ToString());
        }

        var address = sourceEntity.Addresses?.FirstOrDefault(x => x.IsPrimaryAddress) ?? sourceEntity.Addresses?.FirstOrDefault();
        if (address != null)
        {
            destinationEntity.AccountContactAddress = new();
            destinationEntity.AccountContactAddress.Id = Guid.NewGuid();
            destinationEntity.AccountContactAddress.City = address.City;
            destinationEntity.AccountContactAddress.Street = address.Street;
            destinationEntity.AccountContactAddress.Country = address.Country;
            destinationEntity.AccountContactAddress.Province = address.Province;
            destinationEntity.AccountContactAddress.ZipCode = address.ZipCode;
        }

        return destinationEntity;
    }
}
