using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.Sequences;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketNumberGeneratorTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly ISequenceNumberGenerator _sequenceNumberGenerator;

    public TruckTicketNumberGeneratorTask(IProvider<Guid, FacilityEntity> facilityProvider, ISequenceNumberGenerator sequenceNumberGenerator)
    {
        _facilityProvider = facilityProvider;
        _sequenceNumberGenerator = sequenceNumberGenerator;
    }

    public override int RunOrder => 40;

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

        var sequenceType = facility.Type == FacilityType.Lf ? SequenceTypes.ScaleTicket : SequenceTypes.WorkTicket;
        truckTicket.TicketNumber = await _sequenceNumberGenerator.GenerateSequenceNumbers(sequenceType, facility.SiteId, 1).FirstAsync();

        SetTruckTicketType(truckTicket);

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var truckTicket = context.Target;

        var shouldRun = string.IsNullOrEmpty(truckTicket.TicketNumber) && truckTicket.FacilityId != default;

        return Task.FromResult(shouldRun);
    }

    private void SetTruckTicketType(TruckTicketEntity truckTicket)
    {
        // characters after last hyphen is ticket type
        var ticketNumberTicketType = truckTicket.TicketNumber?[(truckTicket.TicketNumber.LastIndexOf('-') + 1)..];
        if (ticketNumberTicketType != null && ticketNumberTicketType.Equals(TruckTicketType.LF.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            truckTicket.TruckTicketType = TruckTicketType.LF;
        }
        else if (ticketNumberTicketType != null && ticketNumberTicketType.Equals(TruckTicketType.SP.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            truckTicket.TruckTicketType = TruckTicketType.SP;
        }
        else if (ticketNumberTicketType != null && ticketNumberTicketType.Equals(TruckTicketType.WT.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            truckTicket.TruckTicketType = TruckTicketType.WT;
        }
        else
        {
            truckTicket.TruckTicketType = TruckTicketType.Undefined;
        }
    }
}
