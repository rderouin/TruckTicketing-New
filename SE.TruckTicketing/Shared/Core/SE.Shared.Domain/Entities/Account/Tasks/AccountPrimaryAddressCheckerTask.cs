using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.Account.Tasks;

public class AccountPrimaryAddressCheckerTask : WorkflowTaskBase<BusinessContext<AccountEntity>>
{
    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<AccountEntity> context)
    {
        //Case 1 - New Address Added as Primary and existing Primary Address already exists
        //Case 2 - All New Addresses added with more than 1 set as Primary
        //Case 3 - No address exists as a Primary address; but addresses exists
        //Case 4 - All New Addresses added with no address set as primary

        if (context.Target.AccountAddresses.Count(x => x.IsPrimaryAddress) > 1)
        {
            if (context.Original.AccountAddresses.Count == 0)
            {
                foreach (var accountAddress in context.Target.AccountAddresses.Where(x => x.IsPrimaryAddress))
                {
                    accountAddress.IsPrimaryAddress = false;
                }
            }
            else
            {
                foreach (var accountAddress in context.Target.AccountAddresses.Where(x => x.Id != default))
                {
                    accountAddress.IsPrimaryAddress = false;
                }
            }

            UpdatePrimaryAddressForTargetAccountAddresses(context);
        }
        else if (context.Target.AccountAddresses.Count(x => x.IsPrimaryAddress) == 0)
        {
            UpdatePrimaryAddressForTargetAccountAddresses(context);
        }

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<AccountEntity> context)
    {
        return Task.FromResult(false);
    }

    private void UpdatePrimaryAddressForTargetAccountAddresses(BusinessContext<AccountEntity> context)
    {
        if (context.Target.AccountAddresses.Count(x => x.Id == default) > 0)
        {
            context.Target.AccountAddresses.Where(x => x.Id == default).First().IsPrimaryAddress = true;
        }
        else
        {
            context.Target.AccountAddresses.First().IsPrimaryAddress = true;
        }
    }
}
