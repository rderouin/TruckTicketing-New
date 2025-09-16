using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.SourceLocation.Tasks;

public class SourceLocationGeneratorContactIndexer : WorkflowTaskBase<BusinessContext<SourceLocationEntity>>
{
    private const string EntityName = "Source Location";

    private readonly IProvider<Guid, AccountContactReferenceIndexEntity> _indexProvider;

    private readonly List<AccountContactReferenceIndexEntity> _sourceLocationGeneratorContactIndex = new();

    public SourceLocationGeneratorContactIndexer(IProvider<Guid, AccountContactReferenceIndexEntity> indexProvider)
    {
        _indexProvider = indexProvider;
    }

    public override int RunOrder => 20;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<SourceLocationEntity> context)
    {
        if (context.Target is null)
        {
            return await Task.FromResult(false);
        }

        var targetEntity = context.Target;
        var originalEntity = context.Original;

        if (IsGeneratorProductionAccountContactUpdated(context) &&
            !_sourceLocationGeneratorContactIndex.Any(x => x.AccountId == targetEntity.GeneratorId && x.AccountContactId != targetEntity.GeneratorProductionAccountContactId))
        {
            await HandleRun(context, originalEntity?.GeneratorId, targetEntity.GeneratorId, originalEntity?.GeneratorProductionAccountContactId,
                            targetEntity.GeneratorProductionAccountContactId);
        }

        if (IsContractOperatorProductionAccountContactUpdated(context) &&
            !_sourceLocationGeneratorContactIndex.Any(x => x.AccountId == targetEntity.ContractOperatorId && x.AccountContactId != context.Target.ContractOperatorProductionAccountContactId))
        {
            await HandleRun(context, originalEntity?.ContractOperatorId, targetEntity.ContractOperatorId, originalEntity?.ContractOperatorProductionAccountContactId,
                            targetEntity.ContractOperatorProductionAccountContactId);
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<SourceLocationEntity> context)
    {
        var shouldRun = context.Target != null && (IsGeneratorProductionAccountContactUpdated(context) || IsContractOperatorProductionAccountContactUpdated(context));

        return Task.FromResult(shouldRun);
    }

    private bool IsGeneratorProductionAccountContactUpdated(BusinessContext<SourceLocationEntity> context)
    {
        return context.Target.GeneratorProductionAccountContactId != null && context.Target.GeneratorProductionAccountContactId != Guid.Empty &&
               context.Original?.GeneratorProductionAccountContactId != context.Target.GeneratorProductionAccountContactId;
    }

    private bool IsContractOperatorProductionAccountContactUpdated(BusinessContext<SourceLocationEntity> context)
    {
        return context.Target.ContractOperatorProductionAccountContactId != null && context.Target.ContractOperatorProductionAccountContactId != Guid.Empty &&
               context.Original?.ContractOperatorProductionAccountContactId != context.Target.ContractOperatorProductionAccountContactId;
    }

    private async Task HandleRun(BusinessContext<SourceLocationEntity> context, Guid? originalAccountId, Guid? targetAccountId, Guid? originalAccountContactId, Guid? targetAccountContactId)
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
            _sourceLocationGeneratorContactIndex.AddRange(updatedContactIndices);
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
        _sourceLocationGeneratorContactIndex.Add(truckTicketBillingCustomerContactIndex);
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
