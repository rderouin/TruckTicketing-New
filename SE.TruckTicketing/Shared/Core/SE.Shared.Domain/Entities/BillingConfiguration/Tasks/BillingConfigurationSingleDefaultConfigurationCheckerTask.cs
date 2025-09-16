using System;
using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.BillingConfiguration.Tasks;

public class BillingConfigurationSingleDefaultConfigurationCheckerTask : WorkflowTaskBase<BusinessContext<BillingConfigurationEntity>>
{
    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigurationProvider;

    public BillingConfigurationSingleDefaultConfigurationCheckerTask(IProvider<Guid, BillingConfigurationEntity> provider)
    {
        _billingConfigurationProvider = provider;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<BillingConfigurationEntity> context)
    {
        var currentDefaultConfigurationForCustomer = await _billingConfigurationProvider.Get(config =>
                                                                                                 config.Id != context.Target.Id &&
                                                                                                 config.BillingCustomerAccountId == context.Target.BillingCustomerAccountId &&
                                                                                                 config.CustomerGeneratorId == context.Target.CustomerGeneratorId &&
                                                                                                 config.IsDefaultConfiguration);

        if (currentDefaultConfigurationForCustomer.Any())
        {
            var updatedEntity = currentDefaultConfigurationForCustomer.First();
            updatedEntity.IsDefaultConfiguration = false;
            await _billingConfigurationProvider.Update(updatedEntity, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<BillingConfigurationEntity> context)
    {
        return Task.FromResult(context.Target.IsDefaultConfiguration);
    }
}
