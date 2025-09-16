using System.Threading.Tasks;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.InvoiceConfiguration.Tasks;

public class GenerateInvoiceConfigurationHashTask : WorkflowTaskBase<BusinessContext<InvoiceConfigurationEntity>>
{
    public override int RunOrder => 5;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<InvoiceConfigurationEntity> context)
    {
        context.Target.ComputeHash();
        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<InvoiceConfigurationEntity> context)
    {
        return Task.FromResult(context.Target is not null);
    }
}
