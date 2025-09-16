using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketTareWeightIndexerTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    public TruckTicketTareWeightIndexerTask(IProvider<Guid, TruckTicketTareWeightEntity> indexProvider)
    {
        _indexProvider = indexProvider;
    }

    private IProvider<Guid, TruckTicketTareWeightEntity> _indexProvider { get; }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var ticket = context.Target;

        var index = (await _indexProvider.Get(entity => entity.FacilityId == ticket.FacilityId
                                                     && entity.TruckNumber == ticket.TruckNumber
                                                     && entity.TrailerNumber == ticket.TrailerNumber
                                                     && entity.TruckingCompanyName == ticket.TruckingCompanyName))
           .MaxBy(entity => entity.LoadDate);

        if (index == null || !index.TareWeight.Equals(ticket.TareWeight))
        {
            await _indexProvider.Insert(new()
            {
                Id = Guid.NewGuid(),
                FacilityId = ticket.FacilityId,
                FacilityName = ticket.FacilityName,
                TruckNumber = ticket.TruckNumber,
                TrailerNumber = ticket.TrailerNumber ?? string.Empty,
                TruckingCompanyName = ticket.TruckingCompanyName,
                TruckingCompanyId = ticket.TruckingCompanyId,
                LoadDate = DateTimeOffset.UtcNow,
                TareWeight = ticket.TareWeight,
                IsActivated = true,
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
            }, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var ticket = context.Target;
        var startTime = DateTimeOffset.UtcNow.Date.AddHours(7);
        return Task.FromResult(ticket != null &&
                               ticket.FacilityType == FacilityType.Lf &&
                               ticket.FacilityId != default &&
                               ticket.LoadDate != default &&
                               ticket.TruckNumber != default &&
                               ticket.TruckingCompanyName != default &&
                               ticket.LoadDate >= startTime &&
                               ticket.LoadDate <= startTime.AddDays(1));
    }
}
