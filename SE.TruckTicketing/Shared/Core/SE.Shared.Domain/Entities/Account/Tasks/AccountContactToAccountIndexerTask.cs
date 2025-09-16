using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Account.Tasks;

public class AccountContactToAccountIndexerTask : WorkflowTaskBase<BusinessContext<AccountEntity>>
{
    private readonly IManager<Guid, AccountContactIndexEntity> _accountContactIndexManager;

    public AccountContactToAccountIndexerTask(IManager<Guid, AccountContactIndexEntity> accountContactIndexManager)
    {
        _accountContactIndexManager = accountContactIndexManager;
    }

    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.AfterInsert | OperationStage.AfterUpdate;

    public override async Task<bool> Run(BusinessContext<AccountEntity> context)
    {
        if (context.Target is null)
        {
            return await Task.FromResult(false);
        }

        var addedUpdatedContacts = ContactAddedOrUpdated(context);
        var addedUpdatedContactIds = new HashSet<Guid>(addedUpdatedContacts.Select(x => x.Id));
        var deletedContactIds = new HashSet<Guid>(addedUpdatedContacts.Where(contact => contact.IsDeleted).Select(x => x.Id));

        var existingIndices = context.Original != null ? (await _accountContactIndexManager.Get(index => addedUpdatedContactIds.Contains(index.Id)))?.ToList() ?? new() : new();

        UpdateExistingContactIndices(existingIndices, addedUpdatedContacts);
        if (deletedContactIds.Any())
        {
            var deletedIndices = existingIndices.Where(index => deletedContactIds.Contains(index.Id)).ToList();
            await CleanupDeletedContactIndices(deletedIndices);
            existingIndices.RemoveAll(index => deletedContactIds.Contains(index.Id));
        }

        var existingAccountContactIds = new HashSet<Guid>(existingIndices.Select(index => index.Id));
        var newIndices = addedUpdatedContacts.Where(c => !existingAccountContactIds.Contains(c.Id) && !c.IsDeleted)
                                             .Select(contact => new AccountContactIndexEntity
                                              {
                                                  Id = contact.Id,
                                                  AccountId = context.Target.Id,
                                                  IsPrimaryAccountContact = contact.IsPrimaryAccountContact,
                                                  IsActive = contact.IsActive,
                                                  Name = contact.Name,
                                                  LastName = contact.LastName,
                                                  Email = contact.Email,
                                                  PhoneNumber = contact.PhoneNumber,
                                                  JobTitle = contact.JobTitle,
                                                  Contact = contact.Contact,
                                                  Street = contact.AccountContactAddress?.Street,
                                                  City = contact.AccountContactAddress?.City,
                                                  ZipCode = contact.AccountContactAddress?.ZipCode,
                                                  Country = contact.AccountContactAddress?.Country ?? CountryCode.Undefined,
                                                  Province = contact.AccountContactAddress?.Province ?? StateProvince.Unspecified,
                                                  ContactFunctions = contact.ContactFunctions,
                                                  SignatoryType = contact.SignatoryType,
                                              });

        var indices = existingIndices.Concat(newIndices).ToArray();
        if (indices.Any())
        {
            await _accountContactIndexManager.BulkSave(indices);
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<AccountEntity> context)
    {
        var anyContactExist = context.Target is { Contacts: { } } && context.Target.Contacts.Any();
        return Task.FromResult((context.Operation == Operation.Insert && anyContactExist) || (context.Operation == Operation.Update && ContactAddedOrUpdated(context).Any()));
    }

    public List<AccountContactEntity> ContactAddedOrUpdated(BusinessContext<AccountEntity> context)
    {
        var contacts = context.Target.Contacts ?? new();
        var targetContacts = new List<AccountContactEntity>();
        //Only send contacts which are newly added or updated; contacts with no update will be sent excluded
        foreach (var contact in contacts)
        {
            var originalContextContact = context.Original?.Contacts?.FirstOrDefault(x => x.Id == contact.Id, new());
            //If new contact added or existing contact updated => run task
            if (originalContextContact == null || originalContextContact.Id == default)
            {
                targetContacts.Add(contact);
                continue;
            }

            //Keep only updated contact on payload
            if (IsContactUpdated(originalContextContact, contact))
            {
                targetContacts.Add(contact);
            }
        }

        return targetContacts;
    }

    private bool IsContactUpdated(AccountContactEntity original, AccountContactEntity target)
    {
        var originalContact = original.Clone();
        originalContact.ContactFunctions.Key = Guid.Empty;
        var targetContact = target.Clone();
        targetContact.ContactFunctions.Key = Guid.Empty;

        return string.CompareOrdinal(originalContact.ToJson(), targetContact.ToJson()) != 0;
    }

    private async Task CleanupDeletedContactIndices(List<AccountContactIndexEntity> deletedAccountContacts)
    {
        if (deletedAccountContacts.Any())
        {
            foreach (var index in deletedAccountContacts)
            {
                await _accountContactIndexManager.Delete(index);
            }
        }
    }

    private void UpdateExistingContactIndices(List<AccountContactIndexEntity> existingContacts, List<AccountContactEntity> updatedContacts)
    {
        foreach (var existingContact in existingContacts)
        {
            var updatedContact = updatedContacts.FirstOrDefault(x => x.Id == existingContact.Id);
            if (updatedContact != null)
            {
                existingContact.IsPrimaryAccountContact = updatedContact.IsPrimaryAccountContact;
                existingContact.IsActive = updatedContact.IsActive;
                existingContact.Name = updatedContact.Name;
                existingContact.LastName = updatedContact.LastName;
                existingContact.Email = updatedContact.Email;
                existingContact.PhoneNumber = updatedContact.PhoneNumber;
                existingContact.JobTitle = updatedContact.JobTitle;
                existingContact.Contact = updatedContact.Contact;
                existingContact.Street = updatedContact.AccountContactAddress?.Street;
                existingContact.City = updatedContact.AccountContactAddress?.City;
                existingContact.ZipCode = updatedContact.AccountContactAddress?.ZipCode;
                existingContact.Country = updatedContact.AccountContactAddress?.Country ?? CountryCode.Undefined;
                existingContact.Province = updatedContact.AccountContactAddress?.Province ?? StateProvince.Unspecified;
                existingContact.ContactFunctions = updatedContact.ContactFunctions;
                existingContact.SignatoryType = updatedContact.SignatoryType;
            }
        }
    }
}
