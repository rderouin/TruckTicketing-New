using System;
using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Account.Tasks;

public class AccountContactAddressSetIdTask : WorkflowTaskBase<BusinessContext<AccountEntity>>
{
    public override int RunOrder => 50;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<AccountEntity> context)
    {
        foreach (var contact in context.Target.Contacts.Where(contact => contact.AccountContactAddress != null && contact.AccountContactAddress.Id == Guid.Empty))
        {
            contact.AccountContactAddress.Id = Guid.NewGuid();
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<AccountEntity> context)
    {
        return Task.FromResult(context.Target.Contacts.Any());
    }
}
