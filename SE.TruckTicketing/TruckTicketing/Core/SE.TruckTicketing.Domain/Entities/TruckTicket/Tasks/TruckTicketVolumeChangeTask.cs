using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Entities.VolumeChange;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Search;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

/// <summary>
///     Task records volume changes
/// </summary>
public class TruckTicketVolumeChangeTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly IProvider<Guid, VolumeChangeEntity> _volumeChangeProvider;

    public TruckTicketVolumeChangeTask(IProvider<Guid, VolumeChangeEntity> volumeChangeProvider)
    {
        _volumeChangeProvider = volumeChangeProvider;
    }

    public override int RunOrder => 15;

    public override OperationStage Stage => OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        if (context.Target is null)
        {
            return false;
        }

        var originalTruckTicket = context.Original;
        var truckTicket = context.Target;

        var volumeChange = await GetVolumeChangeByTicketNumber(truckTicket);

        await CreateOrUpdateVolumeChange(volumeChange, truckTicket, originalTruckTicket);

        return await Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        // only run when ticket is being updated and when VolumeChangeReason is not Undefined and ticket is in approved or invoiced or void status
        // Undefined VolumeChangeReason indicates the ticket update does not include volume changes
        // a ticket being inserted will inherently not have volume changes
        return Task.FromResult(context.Operation == Operation.Update &&
                               context.Target.VolumeChangeReason != VolumeChangeReason.Undefined &&
                               context.Target.Status is TruckTicketStatus.Approved or TruckTicketStatus.Invoiced or TruckTicketStatus.Void);
    }

    private async Task<VolumeChangeEntity> GetVolumeChangeByTicketNumber(TruckTicketEntity truckTicket)
    {
        var search = new SearchCriteria();
        search.AddFilter(nameof(VolumeChangeEntity.TicketNumber), truckTicket.TicketNumber);
        var response = await _volumeChangeProvider.Search(search);
        return response.Results.FirstOrDefault();
    }

    private async Task CreateOrUpdateVolumeChange(VolumeChangeEntity volumeChange, TruckTicketEntity truckTicket, TruckTicketEntity originalTruckTicket)
    {
        // we only record original values and most recent updates

        if (volumeChange == null)
        {
            volumeChange = new()
            {
                Id = Guid.NewGuid(),
                TicketDate = truckTicket.LoadDate.GetValueOrDefault(),
                TicketNumber = truckTicket.TicketNumber,
                ProcessOriginal = originalTruckTicket.Stream,
                OilVolumeOriginal = originalTruckTicket.OilVolume,
                WaterVolumeOriginal = originalTruckTicket.WaterVolume,
                SolidVolumeOriginal = originalTruckTicket.SolidVolume,
                TotalVolumeOriginal = originalTruckTicket.TotalVolume,
                ProcessAdjusted = truckTicket.Stream,

                //zero adjusted volumes for void tickets
                OilVolumeAdjusted = truckTicket.Status == TruckTicketStatus.Void ? 0 : truckTicket.OilVolume,
                WaterVolumeAdjusted = truckTicket.Status == TruckTicketStatus.Void ? 0 : truckTicket.WaterVolume,
                SolidVolumeAdjusted = truckTicket.Status == TruckTicketStatus.Void ? 0 : truckTicket.SolidVolume,
                TotalVolumeAdjusted = truckTicket.Status == TruckTicketStatus.Void ? 0 : truckTicket.TotalVolume,
                VolumeChangeReason = truckTicket.VolumeChangeReason,
                VolumeChangeReasonText = truckTicket.VolumeChangeReasonText,
                FacilityId = truckTicket.FacilityId,
                FacilityName = truckTicket.FacilityName,
                TruckTicketStatus = truckTicket.Status,
            };

            await _volumeChangeProvider.Insert(volumeChange, true);
        }
        else
        {
            // do not update original values

            //zero adjusted volumes for void tickets
            volumeChange.OilVolumeAdjusted = truckTicket.Status == TruckTicketStatus.Void ? 0 : truckTicket.OilVolume;
            volumeChange.WaterVolumeAdjusted = truckTicket.Status == TruckTicketStatus.Void ? 0 : truckTicket.WaterVolume;
            volumeChange.SolidVolumeAdjusted = truckTicket.Status == TruckTicketStatus.Void ? 0 : truckTicket.SolidVolume;
            volumeChange.TotalVolumeAdjusted = truckTicket.Status == TruckTicketStatus.Void ? 0 : truckTicket.TotalVolume;

            volumeChange.VolumeChangeReason = truckTicket.VolumeChangeReason;
            volumeChange.VolumeChangeReasonText = truckTicket.VolumeChangeReasonText;

            await _volumeChangeProvider.Update(volumeChange, true);
        }
    }
}
