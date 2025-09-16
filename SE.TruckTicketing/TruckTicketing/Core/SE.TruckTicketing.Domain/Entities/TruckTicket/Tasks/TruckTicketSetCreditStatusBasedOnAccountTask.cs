using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketSetCreditStatusBasedOnAccountTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    public TruckTicketSetCreditStatusBasedOnAccountTask(IProvider<Guid, AccountEntity> accountProvider)
    {
        _accountProvider = accountProvider;
    }

    public override int RunOrder => 35;

    public override OperationStage Stage => OperationStage.BeforeUpdate | OperationStage.BeforeInsert;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var targetEntity = context.Target;

        var accountEntity = await _accountProvider.GetById(targetEntity.BillingCustomerId);

        if (accountEntity != null)
        {
            targetEntity.CustomerCreditStatus = accountEntity.CreditStatus;
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var invalidTruckTicketStatus = new List<TruckTicketStatus>
        {
            TruckTicketStatus.Void,
            TruckTicketStatus.Invoiced,
        };

        return Task.FromResult(!invalidTruckTicketStatus.Contains(context.Target.Status) && context.Target.BillingCustomerId != default);
    }
}
