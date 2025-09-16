using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class ScaleTicketJobAccumulatedTonnageTrackerTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly IManager<Guid, MaterialApprovalEntity> _materialApprovalManager;

    public ScaleTicketJobAccumulatedTonnageTrackerTask(IManager<Guid, MaterialApprovalEntity> materialApprovalManager)
    {
        _materialApprovalManager = materialApprovalManager;
    }

    public override int RunOrder => 50;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var materialApproval = await _materialApprovalManager.GetById(context.Target.MaterialApprovalId);

        if (context.Target.Status is TruckTicketStatus.Void && context.Original?.Status is not TruckTicketStatus.Void)
        {
            materialApproval.AccumulatedTonnage -= context.Target.NetWeight;
        }
        else
        {
            var accumulatedTonnage = context.Target.NetWeight - (context.Original?.NetWeight ?? 0);
            materialApproval.AccumulatedTonnage += accumulatedTonnage;
        }

        await _materialApprovalManager.Save(materialApproval, true);
        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var shouldRun = context.Target.MaterialApprovalId != Guid.Empty &&
                        Math.Abs(context.Target.NetWeight - (context.Original?.NetWeight ?? 0)) > 0.011;

        return Task.FromResult(shouldRun);
    }
}
