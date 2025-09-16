using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketWellClassificationIndexerTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly IProvider<Guid, TruckTicketWellClassificationUsageEntity> _indexProvider;

    public TruckTicketWellClassificationIndexerTask(IProvider<Guid, TruckTicketWellClassificationUsageEntity> indexProvider)
    {
        _indexProvider = indexProvider;
    }

    public override int RunOrder => 60;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var ticket = context.Target;

        if (DateTimeOffset.UtcNow.Subtract(ticket.LoadDate.Value) > TimeSpan.FromDays(2))
        {
            return true;
        }

        var index =
            (await _indexProvider.Get(entity => entity.FacilityId == ticket.FacilityId &&
                                                entity.SourceLocationId == ticket.SourceLocationId)).FirstOrDefault(new TruckTicketWellClassificationUsageEntity
            {
                FacilityId = ticket.FacilityId,
                FacilityName = ticket.FacilityName,
                SourceLocationId = ticket.SourceLocationId,
                SourceLocationIdentifier = ticket.SourceLocationFormatted,
                TruckTicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
            });

        if (index.WellClassification == ticket.WellClassification)
        {
            return true;
        }

        index.Date = DateTimeOffset.UtcNow;
        index.WellClassification = ticket.WellClassification;

        if (index.Id == default)
        {
            index.Id = Guid.NewGuid();
            await _indexProvider.Insert(index, true);
        }
        else
        {
            await _indexProvider.Update(index, true);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var ticket = context.Target;
        return Task.FromResult(ticket != null &&
                               ticket.FacilityId != default &&
                               ticket.SourceLocationId != default &&
                               ticket.WellClassification != default &&
                               ticket.LoadDate != default);
    }
}
