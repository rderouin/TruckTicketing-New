using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.LoadConfirmation.Tasks;

public class LoadConfirmationCreditStatusBasedOnAccountTask : WorkflowTaskBase<BusinessContext<LoadConfirmationEntity>>
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    public LoadConfirmationCreditStatusBasedOnAccountTask(IProvider<Guid, AccountEntity> accountProvider)
    {
        _accountProvider = accountProvider;
    }

    public override int RunOrder => 35;

    public override OperationStage Stage => OperationStage.BeforeUpdate | OperationStage.BeforeInsert;

    public override async Task<bool> Run(BusinessContext<LoadConfirmationEntity> context)
    {
        var targetEntity = context.Target;

        var accountEntity = await _accountProvider.GetById(targetEntity.BillingCustomerId);

        if (accountEntity != null)
        {
            targetEntity.CustomerCreditStatus = accountEntity.CreditStatus;
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<LoadConfirmationEntity> context)
    {
        var invalidStatus = new List<LoadConfirmationStatus>
        {
            LoadConfirmationStatus.Void,
            LoadConfirmationStatus.Posted,
        };

        return Task.FromResult(!invalidStatus.Contains(context.Target.Status) && context.Target.BillingCustomerId != default);
    }
}
