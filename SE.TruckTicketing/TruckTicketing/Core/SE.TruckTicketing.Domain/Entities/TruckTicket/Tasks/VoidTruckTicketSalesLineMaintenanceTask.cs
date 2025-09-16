using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class VoidTruckTicketSalesLineMaintenanceTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    private readonly ISalesLinesPublisher _salesLinesPublisher;

    public VoidTruckTicketSalesLineMaintenanceTask(IProvider<Guid, SalesLineEntity> salesLineProvider, ISalesLinesPublisher salesLinesPublisher)
    {
        _salesLineProvider = salesLineProvider;
        _salesLinesPublisher = salesLinesPublisher;
    }

    public override int RunOrder => 1000;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var truckTicket = context.Target;
        var salesLines = (await _salesLineProvider.Get(sl => sl.TruckTicketId == truckTicket.Id)).ToList(); // PK - XP for SL by TT ID

        foreach (var salesLine in salesLines)
        {
            if (salesLine.Status is SalesLineStatus.Preview)
            {
                await _salesLineProvider.Delete(salesLine, true);
            }
            else
            {
                salesLine.Status = SalesLineStatus.Void;
                await _salesLineProvider.Update(salesLine, true);
            }
        }

        if (context.Original?.Status is TruckTicketStatus.Approved)
        {
            await _salesLinesPublisher.PublishSalesLines(salesLines);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var shouldRun = context.Target.Status is TruckTicketStatus.Void;
        return Task.FromResult(shouldRun);
    }
}
