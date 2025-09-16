using System;
using System.Threading.Tasks;

using SE.Shared.Domain.LegalEntity;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Account.Tasks;

public class SetFlagToShowAccountInTruckTicketing : WorkflowTaskBase<BusinessContext<AccountEntity>>
{
    private readonly IProvider<Guid, LegalEntityEntity> _legalEntityProvider;

    public SetFlagToShowAccountInTruckTicketing(IProvider<Guid, LegalEntityEntity> legalEntityProvider)
    {
        _legalEntityProvider = legalEntityProvider;
    }

    public override int RunOrder => 45;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<AccountEntity> context)
    {
        var accountEntity = context.Target;

        var legalEntity = await _legalEntityProvider.GetById(accountEntity.LegalEntityId);
        if (legalEntity == null)
        {
            return await Task.FromResult(true);
        }

        context.Target.IsShowAccount = legalEntity.ShowAccountsInTruckTicketing;
        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<AccountEntity> context)
    {
        return Task.FromResult(context.Target.LegalEntityId != default);
    }
}
