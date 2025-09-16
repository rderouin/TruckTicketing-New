using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.Sequences;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketTrackingNumberTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly ISequenceNumberGenerator _sequenceNumberGenerator;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    public TruckTicketTrackingNumberTask(ISequenceNumberGenerator sequenceNumberGenerator, IProvider<Guid, FacilityEntity> facilityProvider)
    {
        _sequenceNumberGenerator = sequenceNumberGenerator;
        _facilityProvider = facilityProvider;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        if (context.Target is null)
        {
            return false;
        }

        var truckTicket = context.Target;

        var facility = await _facilityProvider.GetById(truckTicket.FacilityId);
        if (facility is null)
        {
            return false;
        }

        var ticketTrackingDate = GetTrackingDate(truckTicket, facility);
        
        var prefix = $"{facility.SiteId}{ticketTrackingDate?.Date:yyyyMMdd}";
        var trackingNumber = await _sequenceNumberGenerator.GenerateSequenceNumbers(SequenceTypes.LandFillTrackingNumber, prefix, 1).FirstAsync();

        truckTicket.TrackingNumber = trackingNumber.Replace(prefix, string.Empty);
        return await Task.FromResult(true);
    }

    private DateTime? GetTrackingDate(TruckTicketEntity truckTicket, FacilityEntity facility)
    {
        if (!truckTicket.TimeIn.HasValue || !truckTicket.LoadDate.HasValue)
        {
            return null;
        }
        TimeOnly cutOffTime = TimeOnly.FromDateTime((facility.OperatingDayCutOffTime ?? new(DateTime.Today)).DateTime);
        TimeOnly ticketTime = TimeOnly.FromDateTime(truckTicket.TimeIn.Value.DateTime);
        return ticketTime < cutOffTime ? truckTicket.LoadDate.Value.Date.AddDays(-1) : truckTicket.LoadDate.Value.Date;
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var ticket = context.Target;
        return Task.FromResult((string.IsNullOrEmpty(ticket?.TrackingNumber) || (context.Original is not null && !context.Original.LoadDate.Equals(context.Target.LoadDate)))
                            && ticket?.TruckTicketType == TruckTicketType.LF);
    }
}
