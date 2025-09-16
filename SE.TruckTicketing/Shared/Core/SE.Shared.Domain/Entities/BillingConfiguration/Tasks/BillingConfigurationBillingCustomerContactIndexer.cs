using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.BillingConfiguration.Tasks;

public class BillingConfigurationBillingCustomerContactIndexer : WorkflowTaskBase<BusinessContext<BillingConfigurationEntity>>
{
    private const string EntityName = "Billing Configuration";

    private readonly List<AccountContactReferenceIndexEntity> _billingConfigurationContactIndex = new();

    private readonly IProvider<Guid, AccountContactReferenceIndexEntity> _indexProvider;

    public BillingConfigurationBillingCustomerContactIndexer(IProvider<Guid, AccountContactReferenceIndexEntity> indexProvider)
    {
        _indexProvider = indexProvider;
    }

    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<BillingConfigurationEntity> context)
    {
        if (context.Target is null)
        {
            return await Task.FromResult(false);
        }

        var targetEntity = context.Target;
        var originalEntity = context.Original;

        if (IsBillingContactUpdated(context) &&
            !_billingConfigurationContactIndex.Any(x => x.AccountId == targetEntity.BillingCustomerAccountId && x.AccountContactId == targetEntity.BillingContactId))
        {
            await HandleRun(context, originalEntity?.BillingCustomerAccountId, targetEntity.BillingCustomerAccountId, originalEntity?.BillingContactId,
                            targetEntity.BillingContactId);
        }

        if (IsGeneratorRepresentativeUpdated(context) &&
            !_billingConfigurationContactIndex.Any(x => x.AccountId == targetEntity.CustomerGeneratorId && x.AccountContactId == context.Target.GeneratorRepresentativeId))
        {
            await HandleRun(context, originalEntity?.CustomerGeneratorId, targetEntity.CustomerGeneratorId, originalEntity?.GeneratorRepresentativeId,
                            targetEntity.GeneratorRepresentativeId);
        }

        if (IsEmailDeliveryContactsUpdated(context))
        {
            await HandleEmailDeliveryContactIndices(context);
        }

        if (IsSignatoriesUpdated(context))
        {
            await HandleSignatoryIndices(context);
        }

        return true;
    }

    private async Task HandleSignatoryIndices(BusinessContext<BillingConfigurationEntity> context)
    {
        var originalSignatories = context.Original != null && context.Original.Signatories.Any()
                                      ? new HashSet<Guid>(context.Original.Signatories.Select(x => x.AccountContactId))
                                      : new();

        var targetSignatories = new HashSet<Guid>(context.Target.Signatories.Select(x => x.AccountContactId));

        var addedSignatories =
            new HashSet<Guid>(context.Target.Signatories.Where(x => !originalSignatories.Contains(x.AccountContactId)).Select(x => x.AccountContactId));

        var removedSignatories = context.Original != null && context.Original.Signatories.Any()
                                     ? new HashSet<Guid>(context.Original.Signatories.Where(x => !targetSignatories.Contains(x.AccountContactId))
                                                                .Select(x => x.AccountContactId))
                                     : new();

        var disabledSignatories = new HashSet<Guid>(context.Target.Signatories.Where(x => !x.IsAuthorized).Select(x => x.AccountContactId));

        //Handle newly added Signatories
        foreach (var addedSignatory in addedSignatories)
        {
            var signatory = context.Target.Signatories.FirstOrDefault(x => x.AccountContactId == addedSignatory, new());
            if (signatory == null || signatory.Id == default)
            {
                continue;
            }

            await HandleSignatoriesUpdate(context, signatory, false);
        }

        //Handle removed Signatories
        foreach (var removedSignatory in removedSignatories)
        {
            var signatory = context.Original?.Signatories.FirstOrDefault(x => x.AccountContactId == removedSignatory, new());
            if (signatory == null || signatory.Id == default)
            {
                continue;
            }

            await HandleSignatoriesUpdate(context, signatory, true);
        }

        //Handle disabled Signatories
        foreach (var disabledSignatory in disabledSignatories)
        {
            var signatory = context.Target.Signatories.FirstOrDefault(x => x.AccountContactId == disabledSignatory, new());
            if (signatory == null || signatory.Id == default)
            {
                continue;
            }

            await HandleSignatoriesUpdate(context, signatory, true);
        }
    }

    private async Task HandleEmailDeliveryContactIndices(BusinessContext<BillingConfigurationEntity> context)
    {
        var originalEmailDeliveryContacts = context.Original != null && context.Original.EmailDeliveryContacts.Any()
                                                ? new HashSet<Guid>(context.Original.EmailDeliveryContacts.Select(x => x.AccountContactId))
                                                : new();

        var targetEmailDeliveryContacts = new HashSet<Guid>(context.Target.EmailDeliveryContacts.Select(x => x.AccountContactId));

        var addedEmailDeliveryContacts =
            new HashSet<Guid>(context.Target.EmailDeliveryContacts.Where(x => !originalEmailDeliveryContacts.Contains(x.AccountContactId)).Select(x => x.AccountContactId));

        var removedEmailDeliveryContacts = context.Original != null && context.Original.EmailDeliveryContacts.Any()
                                               ? new HashSet<Guid>(context.Original.EmailDeliveryContacts.Where(x => !targetEmailDeliveryContacts.Contains(x.AccountContactId))
                                                                          .Select(x => x.AccountContactId))
                                               : new();

        var disabledEmailDeliveryContacts = new HashSet<Guid>(context.Target.EmailDeliveryContacts.Where(x => !x.IsAuthorized).Select(x => x.AccountContactId));

        //Handle newly added Email Delivery Contact
        foreach (var addedDeliveryContact in addedEmailDeliveryContacts)
        {
            var emailDeliveryContact = context.Target.EmailDeliveryContacts.FirstOrDefault(x => x.AccountContactId == addedDeliveryContact, new());
            if (emailDeliveryContact == null || emailDeliveryContact.Id == default)
            {
                continue;
            }

            await HandleEmailDeliveryContactUpdate(context, emailDeliveryContact, false);
        }

        //Handle removed Email Delivery Contact
        foreach (var removedDeliveryContact in removedEmailDeliveryContacts)
        {
            var emailDeliveryContact = context.Original?.EmailDeliveryContacts.FirstOrDefault(x => x.AccountContactId == removedDeliveryContact, new());
            if (emailDeliveryContact == null || emailDeliveryContact.Id == default)
            {
                continue;
            }

            await HandleEmailDeliveryContactUpdate(context, emailDeliveryContact, true);
        }

        //Handle disabled Email Delivery Contact
        foreach (var disabledDeliveryContact in disabledEmailDeliveryContacts)
        {
            var emailDeliveryContact = context.Target.EmailDeliveryContacts.FirstOrDefault(x => x.AccountContactId == disabledDeliveryContact, new());
            if (emailDeliveryContact == null || emailDeliveryContact.Id == default)
            {
                continue;
            }

            await HandleEmailDeliveryContactUpdate(context, emailDeliveryContact, true);
        }
    }

    private async Task HandleEmailDeliveryContactUpdate(BusinessContext<BillingConfigurationEntity> context, EmailDeliveryContactEntity emailDeliveryContact, bool isDisabled)
    {
        if (!_billingConfigurationContactIndex.Any(x => x.AccountId == context.Target.BillingCustomerAccountId && x.AccountContactId == emailDeliveryContact.AccountContactId))
        {
            var pk = context.Target == null ? null : AccountContactReferenceIndexEntity.GetPartitionKey(context.Target.BillingCustomerAccountId);
            var indices = (await _indexProvider.Get(index => index.ReferenceEntityId == context.Target.Id && index.AccountId == context.Target.BillingCustomerAccountId &&
                                                             index.AccountContactId == emailDeliveryContact.AccountContactId, pk))?.ToList() ?? new(); // PK - OK

            if (indices.Any())
            {
                _billingConfigurationContactIndex.AddRange(indices);
                UpdateAccountContactReferenceIndices(indices, isDisabled);
                return;
            }

            var emailDeliveryContactIndex = new AccountContactReferenceIndexEntity
            {
                Id = Guid.NewGuid(),
                ReferenceEntityId = context.Target.Id,
                AccountContactId = emailDeliveryContact.AccountContactId,
                AccountId = context.Target.BillingCustomerAccountId,
                ReferenceEntityName = EntityName,
                IsDisabled = isDisabled,
            };

            emailDeliveryContactIndex.InitPartitionKey();
            _billingConfigurationContactIndex.Add(emailDeliveryContactIndex);
            await _indexProvider.Insert(emailDeliveryContactIndex, true);
        }
    }

    private async Task HandleSignatoriesUpdate(BusinessContext<BillingConfigurationEntity> context, SignatoryContactEntity signatory, bool isDisabled)
    {
        if (!_billingConfigurationContactIndex.Any(x => x.AccountId == signatory.AccountId && x.AccountContactId == signatory.AccountContactId))
        {
            var pk = signatory == null ? null : AccountContactReferenceIndexEntity.GetPartitionKey(signatory.AccountId);
            var indices = (await _indexProvider.Get(index => index.ReferenceEntityId == context.Target.Id && index.AccountId == signatory.AccountId &&
                                                             index.AccountContactId == signatory.AccountContactId, pk))?.ToList() ?? new(); // PK - OK

            if (indices.Any())
            {
                _billingConfigurationContactIndex.AddRange(indices);
                UpdateAccountContactReferenceIndices(indices, isDisabled);
                return;
            }

            var emailDeliveryContactIndex = new AccountContactReferenceIndexEntity
            {
                Id = Guid.NewGuid(),
                ReferenceEntityId = context.Target.Id,
                AccountContactId = signatory.AccountContactId,
                AccountId = signatory.AccountId,
                ReferenceEntityName = EntityName,
                IsDisabled = isDisabled,
            };

            emailDeliveryContactIndex.InitPartitionKey();
            _billingConfigurationContactIndex.Add(emailDeliveryContactIndex);
            await _indexProvider.Insert(emailDeliveryContactIndex, true);
        }
    }

    public override Task<bool> ShouldRun(BusinessContext<BillingConfigurationEntity> context)
    {
        var shouldRun = context.Target != null && (IsBillingContactUpdated(context) || IsGeneratorRepresentativeUpdated(context) || IsEmailDeliveryContactsUpdated(context) ||
                                                   IsSignatoriesUpdated(context));

        return Task.FromResult(shouldRun);
    }

    private bool IsBillingContactUpdated(BusinessContext<BillingConfigurationEntity> context)
    {
        return context.Target.BillingContactId != null && context.Target.BillingContactId != Guid.Empty &&
               context.Original?.BillingContactId != context.Target.BillingContactId;
    }

    private bool IsGeneratorRepresentativeUpdated(BusinessContext<BillingConfigurationEntity> context)
    {
        return context.Target.GeneratorRepresentativeId != null && context.Target.GeneratorRepresentativeId != Guid.Empty &&
               context.Original?.GeneratorRepresentativeId != context.Target.GeneratorRepresentativeId;
    }

    private bool IsEmailDeliveryContactsUpdated(BusinessContext<BillingConfigurationEntity> context)
    {
        return (context.Original == null && context.Target.EmailDeliveryContacts.Any()) ||
               (context.Original != null && IsEmailDeliveryContactUpdated(context));
    }

    private bool IsSignatoriesUpdated(BusinessContext<BillingConfigurationEntity> context)
    {
        return (context.Original == null && context.Target.Signatories.Any()) ||
               (context.Original != null && IsSignatoryUpdated(context));
    }

    private bool IsSignatoryUpdated(BusinessContext<BillingConfigurationEntity> context)
    {
        var original = context.Original?.Signatories.ToJson();
        var target = context.Target.Signatories.ToJson();
        return string.CompareOrdinal(original, target) != 0;
    }

    private bool IsEmailDeliveryContactUpdated(BusinessContext<BillingConfigurationEntity> context)
    {
        var original = context.Original?.EmailDeliveryContacts.ToJson();
        var target = context.Target.EmailDeliveryContacts.ToJson();
        return string.CompareOrdinal(original, target) != 0;
    }

    private async Task HandleRun(BusinessContext<BillingConfigurationEntity> context,
                                 Guid? originalAccountId,
                                 Guid? targetAccountId,
                                 Guid? originalAccountContactId,
                                 Guid? targetAccountContactId)
    {
        var targetEntity = context.Target;
        var originalEntity = context.Original;

        if (targetAccountId == null || targetAccountId == Guid.Empty || targetAccountContactId == null || targetAccountContactId == Guid.Empty)
        {
            return;
        }

        var shouldGetExistingIndices = context.Original != null && originalAccountId != null && originalAccountId != Guid.Empty && originalAccountContactId != null &&
                                       originalAccountContactId != Guid.Empty;

        var pko = originalAccountId.HasValue ? AccountContactReferenceIndexEntity.GetPartitionKey(originalAccountId.Value) : null;
        var existingIndices = shouldGetExistingIndices
                                  ? (await _indexProvider.Get(index => index.ReferenceEntityId == originalEntity.Id && index.AccountId == originalAccountId &&
                                                                       index.AccountContactId == originalAccountContactId, pko))?.ToList() ?? new() // PK - OK
                                  : new();

        var pkt = AccountContactReferenceIndexEntity.GetPartitionKey(targetAccountId.Value);
        var updatedContactIndices = context.Original != null
                                        ? (await _indexProvider.Get(index => index.ReferenceEntityId == targetEntity.Id && index.AccountId == targetAccountId &&
                                                                             index.AccountContactId == targetAccountContactId, pkt))?.ToList() ?? new() // PK - OK
                                        : new();

        //Account updated on existing MaterialApproval
        if (existingIndices.Any())
        {
            UpdateAccountContactReferenceIndices(existingIndices, true);
        }

        //If updated Account/AccountContact/MaterialApproval index exist but disabled; enable existing 
        if (updatedContactIndices.Any())
        {
            _billingConfigurationContactIndex.AddRange(updatedContactIndices);
            UpdateAccountContactReferenceIndices(updatedContactIndices, false);

            return;
        }

        var truckTicketBillingCustomerContactIndex = new AccountContactReferenceIndexEntity
        {
            Id = Guid.NewGuid(),
            ReferenceEntityId = context.Target.Id,
            AccountContactId = targetAccountContactId,
            AccountId = targetAccountId.Value,
            ReferenceEntityName = EntityName,
            IsDisabled = false,
        };

        truckTicketBillingCustomerContactIndex.InitPartitionKey();
        _billingConfigurationContactIndex.Add(truckTicketBillingCustomerContactIndex);
        await _indexProvider.Insert(truckTicketBillingCustomerContactIndex, true);
    }

    private async void UpdateAccountContactReferenceIndices(List<AccountContactReferenceIndexEntity> indices, bool isDisabled)
    {
        foreach (var index in indices)
        {
            index.IsDisabled = isDisabled;
            await _indexProvider.Update(index, true);
        }
    }
}
