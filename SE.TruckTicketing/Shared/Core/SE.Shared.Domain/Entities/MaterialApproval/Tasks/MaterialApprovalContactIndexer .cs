using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.MaterialApproval.Tasks;

public class MaterialApprovalContactIndexer : WorkflowTaskBase<BusinessContext<MaterialApprovalEntity>>
{
    private const string EntityName = "Material Approval";

    private readonly IProvider<Guid, AccountContactReferenceIndexEntity> _indexProvider;

    private readonly List<AccountContactReferenceIndexEntity> _materialApprovalContactReferenceIndexEntities = new();

    public MaterialApprovalContactIndexer(IProvider<Guid, AccountContactReferenceIndexEntity> indexProvider)
    {
        _indexProvider = indexProvider;
    }

    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<MaterialApprovalEntity> context)
    {
        if (context.Target is null)
        {
            return await Task.FromResult(false);
        }

        var targetEntity = context.Target;
        var originalEntity = context.Original;

        if (IsBillingCustomerContactUpdated(context) &&
            !_materialApprovalContactReferenceIndexEntities.Any(x => x.AccountId == targetEntity.BillingCustomerId && x.AccountContactId == targetEntity.BillingCustomerContactId))
        {
            await HandleRun(context, originalEntity?.BillingCustomerId, targetEntity.BillingCustomerId, originalEntity?.BillingCustomerContactId, targetEntity.BillingCustomerContactId);
        }

        if (IsThirdPartyAnalyticalContactUpdated(context) &&
            !_materialApprovalContactReferenceIndexEntities.Any(x => x.AccountId == targetEntity.ThirdPartyAnalyticalCompanyId &&
                                                                     x.AccountContactId == targetEntity.ThirdPartyAnalyticalCompanyContactId))
        {
            await HandleRun(context, originalEntity?.ThirdPartyAnalyticalCompanyId, targetEntity.ThirdPartyAnalyticalCompanyId, originalEntity?.ThirdPartyAnalyticalCompanyContactId,
                            targetEntity.ThirdPartyAnalyticalCompanyContactId);
        }

        if (IsTruckingCompanyContactUpdated(context) && !_materialApprovalContactReferenceIndexEntities.Any(x => x.AccountId == targetEntity.TruckingCompanyId &&
                                                                                                                 x.AccountContactId == targetEntity.TruckingCompanyContactId))
        {
            await HandleRun(context, originalEntity?.TruckingCompanyId, targetEntity.TruckingCompanyId, originalEntity?.TruckingCompanyContactId, targetEntity.TruckingCompanyContactId);
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<MaterialApprovalEntity> context)
    {
        var shouldRun = context.Target != null && (IsBillingCustomerContactUpdated(context) || IsThirdPartyAnalyticalContactUpdated(context) || IsTruckingCompanyContactUpdated(context));

        return Task.FromResult(shouldRun);
    }

    private bool IsBillingCustomerContactUpdated(BusinessContext<MaterialApprovalEntity> context)
    {
        return context.Target.BillingCustomerContactId != null && context.Target.BillingCustomerContactId != Guid.Empty &&
               context.Original?.BillingCustomerContactId != context.Target.BillingCustomerContactId;
    }

    private bool IsThirdPartyAnalyticalContactUpdated(BusinessContext<MaterialApprovalEntity> context)
    {
        return context.Target.ThirdPartyAnalyticalCompanyContactId != null && context.Target.ThirdPartyAnalyticalCompanyContactId != Guid.Empty &&
               context.Original?.ThirdPartyAnalyticalCompanyContactId != context.Target.ThirdPartyAnalyticalCompanyContactId;
    }

    private bool IsTruckingCompanyContactUpdated(BusinessContext<MaterialApprovalEntity> context)
    {
        return context.Target.TruckingCompanyContactId != null && context.Target.TruckingCompanyContactId != Guid.Empty &&
               context.Original?.TruckingCompanyContactId != context.Target.TruckingCompanyContactId;
    }

    private async Task HandleRun(BusinessContext<MaterialApprovalEntity> context, Guid? originalAccountId, Guid? targetAccountId, Guid? originalAccountContactId, Guid? targetAccountContactId)
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
            _materialApprovalContactReferenceIndexEntities.AddRange(updatedContactIndices);
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
        _materialApprovalContactReferenceIndexEntities.Add(truckTicketBillingCustomerContactIndex);
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
