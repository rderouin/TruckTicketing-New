using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Invoices;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.Invoices.Tasks;

public class InvoiceCreditStatusBasedOnAccountTask : WorkflowTaskBase<BusinessContext<InvoiceEntity>>
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    public InvoiceCreditStatusBasedOnAccountTask(IProvider<Guid, AccountEntity> accountProvider)
    {
        _accountProvider = accountProvider;
    }

    public override int RunOrder => 35;

    public override OperationStage Stage => OperationStage.BeforeUpdate | OperationStage.BeforeInsert;

    public override async Task<bool> Run(BusinessContext<InvoiceEntity> context)
    {
        var targetEntity = context.Target;

        var accountEntity = await _accountProvider.GetById(targetEntity.CustomerId);

        if (accountEntity != null)
        {
            targetEntity.CustomerCreditStatus = accountEntity.CreditStatus;
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<InvoiceEntity> context)
    {
        var validStatus = new List<InvoiceStatus>
        {
            InvoiceStatus.UnPosted,
            InvoiceStatus.Posted,
            InvoiceStatus.AgingUnSent,
        };

        return Task.FromResult(validStatus.Contains(context.Target.Status) && context.Target.CustomerId != default);
    }
}
