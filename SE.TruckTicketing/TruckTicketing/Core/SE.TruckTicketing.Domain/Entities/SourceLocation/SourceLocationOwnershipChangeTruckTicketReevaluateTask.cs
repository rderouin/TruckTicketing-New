using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.SourceLocation;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.SourceLocation;

public class SourceLocationOwnershipChangeTruckTicketReevaluateTask : WorkflowTaskBase<BusinessContext<SourceLocationEntity>>
{
    private readonly ITruckTicketManager _truckTicketManager;

    public SourceLocationOwnershipChangeTruckTicketReevaluateTask(ITruckTicketManager truckTicketManager)
    {
        _truckTicketManager = truckTicketManager;
    }

    public override int RunOrder => 50;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<SourceLocationEntity> context)
    {
        var startDate = context.Target.GeneratorStartDate.Date;
        
        var truckTickets = await _truckTicketManager.Get(ticket => ticket.SourceLocationId == context.Target.Id &&
                                                                   ticket.Status == TruckTicketStatus.Open &&
                                                                   ticket.EffectiveDate >= startDate);

        foreach (var ticket in truckTickets)
        {
            ticket.GeneratorId = context.Target.GeneratorId;
            ticket.GeneratorName = context.Target.GeneratorName;

            ticket.BillingConfigurationId = default;
            ticket.BillingCustomerId = Guid.Empty;
            ticket.BillingCustomerName = default;
            ticket.Signatories = new();
            ticket.BillingContact = default;
            ticket.EdiFieldValues = new();

            await _truckTicketManager.Update(ticket, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<SourceLocationEntity> context)
    {
        return Task.FromResult(context.Original != null && context.Operation == Operation.Update && context.Target.GeneratorId != context.Original.GeneratorId);
    }
}
