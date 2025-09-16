using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Tasks;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Extensions;

using AccountTypes = SE.TruckTicketing.Contracts.Lookups.AccountTypes;

namespace SE.Shared.Domain.Entities.Invoices;

public class InvoiceEntityPublishMessageTask : IEntityPublishMessageTask<InvoiceEntity>
{
    public Task<bool> ShouldPublishMessage(BusinessContext<InvoiceEntity> context)
    {
        var publishMessage = context.Target.Status is not InvoiceStatus.Unknown;

        return Task.FromResult(publishMessage);
    }

    public Task EnrichEnvelopeModel(EntityEnvelopeModel<InvoiceEntity> model)
    {
        if (model.Payload.Status is InvoiceStatus.UnPosted or InvoiceStatus.Void)
        {
            model.MessageType = MessageType.SalesOrder.ToString();
        }

        return Task.CompletedTask;
    }

    public Task<string> GetSessionIdForMessage(BusinessContext<InvoiceEntity> context)
    {
        var defaultSessionId = context.Target.Id.ToString();
        return Task.FromResult(defaultSessionId);
    }
}

public class AccountEntityPublishMessageTask : IEntityPublishMessageTask<AccountEntity>
{
    public Task<string> GetSessionIdForMessage(BusinessContext<AccountEntity> context)
    {
        return Task.FromResult(context.Target.Id.ToString());
    }

    public Task EnrichEnvelopeModel(EntityEnvelopeModel<AccountEntity> model)
    {
        model.Payload = model.Payload.Clone();
        var account = model.Payload;
        var contacts = account.Contacts ?? new();

        foreach (var contact in contacts.Where(contact => string.IsNullOrWhiteSpace(contact.AccountContactAddress?.Street) ||
                                                          string.IsNullOrWhiteSpace(contact.AccountContactAddress?.City) ||
                                                          string.IsNullOrWhiteSpace(contact.AccountContactAddress?.ZipCode) ||
                                                          contact.AccountContactAddress?.Province is StateProvince.Unspecified ||
                                                          contact.AccountContactAddress?.Country is CountryCode.Undefined))
        {
            contact.AccountContactAddress = null;
        }

        if (account.AccountTypes?.List?.Contains(AccountTypes.Customer) is true && account.CreditStatus is CreditStatus.Undefined)
        {
            account.CreditStatus = CreditStatus.Pending;
        }

        return Task.CompletedTask;
    }

    public Task<bool> ShouldPublishMessage(BusinessContext<AccountEntity> context)
    {
        return Task.FromResult(true);
    }

    public Task<AccountEntity> EvaluateEntityForUpdates(BusinessContext<AccountEntity> context)
    {
        if (context.Original == null)
        {
            return Task.FromResult(context.Target);
        }

        var targetEntity = context.Target.Clone();
        targetEntity.Contacts = new();
        var contacts = context.Target.Contacts ?? new();

        //Only send contacts which are newly added or updated; contacts with no update will be sent excluded
        foreach (var contact in contacts)
        {
            var originalContextContact = context.Original.Contacts.FirstOrDefault(x => x.Id == contact.Id, new());
            //If new contact added; keep it with payload
            if (originalContextContact.Id == default)
            {
                targetEntity.Contacts.Add(contact);
                continue;
            }

            //Keep only updated contact on payload
            if (IsContactUpdated(originalContextContact, contact))
            {
                targetEntity.Contacts.Add(contact);
            }
        }

        return Task.FromResult(targetEntity);
    }

    private bool IsContactUpdated(AccountContactEntity original, AccountContactEntity target)
    {
        var originalContact = original.Clone();
        originalContact.ContactFunctions.Key = Guid.Empty;
        var targetContact = target.Clone();
        targetContact.ContactFunctions.Key = Guid.Empty;

        return string.CompareOrdinal(originalContact.ToJson(), targetContact.ToJson()) != 0;
    }
}
