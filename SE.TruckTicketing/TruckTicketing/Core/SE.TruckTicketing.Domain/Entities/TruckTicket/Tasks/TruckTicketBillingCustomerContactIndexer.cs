using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketBillingCustomerContactIndexer : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private const string EntityName = "Truck Ticket";

    private readonly IProvider<Guid, AccountContactReferenceIndexEntity> _indexProvider;

    public TruckTicketBillingCustomerContactIndexer(IProvider<Guid, AccountContactReferenceIndexEntity> indexProvider)
    {
        _indexProvider = indexProvider;
    }

    public override int RunOrder => 20;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        if (context.Target is null)
        {
            return await Task.FromResult(false);
        }

        var targetEntity = context.Target;
        var originalEntity = context.Original;

        if (targetEntity.BillingCustomerId == Guid.Empty || targetEntity.BillingContact?.AccountContactId == null ||
            targetEntity.BillingContact.AccountContactId == Guid.Empty)
        {
            return await Task.FromResult(true);
        }

        var shouldGetExistingIndices = context.Original != null && originalEntity.BillingCustomerId != Guid.Empty && originalEntity.BillingContact is { AccountContactId: { } } &&
                                       originalEntity.BillingContact.AccountContactId != Guid.Empty;

        var opk = originalEntity == null ? null : AccountContactReferenceIndexEntity.GetPartitionKey(originalEntity.BillingCustomerId);
        var existingIndices = shouldGetExistingIndices
                                  ? (await _indexProvider.Get(index => index.ReferenceEntityId == originalEntity.Id && index.AccountId == originalEntity.BillingCustomerId &&
                                                                       index.AccountContactId == originalEntity.BillingContact.AccountContactId, opk))?.ToList() ?? new()
                                  : new(); // PK - OK

        var tpk = targetEntity == null ? null : AccountContactReferenceIndexEntity.GetPartitionKey(targetEntity.BillingCustomerId);
        var updatedContactIndices = context.Original != null
                                        ? (await _indexProvider.Get(index => index.ReferenceEntityId == targetEntity.Id && index.AccountId == targetEntity.BillingCustomerId &&
                                                                             index.AccountContactId == targetEntity.BillingContact.AccountContactId, tpk))?.ToList() ?? new()
                                        : new(); // PK - OK

        // Billing Customer updated on existing TruckTicket
        if (existingIndices.Any())
        {
            UpdateAccountContactReferenceIndices(existingIndices, true);
        }

        // If updated Customer/TruckTicket/BillingContact index exist but disabled; enable existing 
        if (updatedContactIndices.Any())
        {
            UpdateAccountContactReferenceIndices(updatedContactIndices, false);

            return true;
        }

        var truckTicketBillingCustomerContactIndex = new AccountContactReferenceIndexEntity
        {
            Id = Guid.NewGuid(),
            ReferenceEntityId = context.Target.Id,
            AccountContactId = context.Target.BillingContact.AccountContactId,
            AccountId = context.Target.BillingCustomerId,
            ReferenceEntityName = EntityName,
            IsDisabled = false,
        };

        truckTicketBillingCustomerContactIndex.InitPartitionKey();

        await _indexProvider.Insert(truckTicketBillingCustomerContactIndex, true);

        return true;
    }

    private async void UpdateAccountContactReferenceIndices(List<AccountContactReferenceIndexEntity> indices, bool isDisabled)
    {
        foreach (var index in indices)
        {
            index.IsDisabled = isDisabled;
            await _indexProvider.Update(index, true);
        }
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var shouldRun = context.Target != null && context.Target.BillingCustomerId != Guid.Empty && context.Target.BillingContact?.AccountContactId is { } &&
                        context.Original?.BillingContact?.AccountContactId != context.Target.BillingContact.AccountContactId;

        return Task.FromResult(shouldRun);
    }
}
