using System.Threading.Tasks;

using Trident.Business;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.MaterialApproval.Tasks;

public class MaterialApprovalAnalyticalDateTask : WorkflowTaskBase<BusinessContext<MaterialApprovalEntity>>
{
    public override int RunOrder => 1100;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override Task<bool> Run(BusinessContext<MaterialApprovalEntity> context)
    {
        if (context.Target.AnalyticalExpiryDate != context.Original.AnalyticalExpiryDate)
        {
            context.Target.AnalyticalExpiryDatePrevious = context.Original.AnalyticalExpiryDate;
        }

        return Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<MaterialApprovalEntity> context)
    {
        return Task.FromResult(context.Operation == Operation.Update);
    }
}
