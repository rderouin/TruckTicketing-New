using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public class TruckTicketServiceTypeVolumeEnricherTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>
{
    private readonly IProvider<Guid, ServiceTypeEntity> _serviceTypeProvider;

    public TruckTicketServiceTypeVolumeEnricherTask(IProvider<Guid, ServiceTypeEntity> serviceTypeProvider)
    {
        _serviceTypeProvider = serviceTypeProvider;
    }

    public override int RunOrder => 1;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var truckTicket = context.Target;

        truckTicket.OilVolume = default;
        truckTicket.WaterVolume = default;
        truckTicket.SolidVolume = default;

        truckTicket.OilVolumePercent = default;
        truckTicket.WaterVolumePercent = default;
        truckTicket.SolidVolumePercent = default;

        switch (truckTicket.ReportAsCutType)
        {
            case ReportAsCutTypes.Oil:
                truckTicket.OilVolume = truckTicket.TotalVolume;
                truckTicket.OilVolumePercent = 100;
                break;
            case ReportAsCutTypes.Water:
                truckTicket.WaterVolume = truckTicket.TotalVolume;
                truckTicket.WaterVolumePercent = 100;
                break;
            case ReportAsCutTypes.Solids:
                truckTicket.SolidVolume = truckTicket.TotalVolume;
                truckTicket.SolidVolumePercent = 100;
                break;
        }

        return Task.FromResult(true);
    }

    public override async Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var isManualTicket = context.Target.TruckTicketType is TruckTicketType.WT or TruckTicketType.SP;

        var ticketHasRequiredFields = context.Target.ServiceTypeId.HasValue && context.Target.TotalVolume > 0;
        var serviceTypeChanged = context.Original?.ServiceTypeId != context.Target.ServiceTypeId;
        var totalVolumeChanged = Math.Abs((context.Original?.TotalVolume ?? 0) - context.Target.TotalVolume) > 0.01;

        var shouldRun = isManualTicket && ticketHasRequiredFields && (serviceTypeChanged || totalVolumeChanged);

        if (!shouldRun)
        {
            return false;
        }

        var serviceType = await _serviceTypeProvider.GetById(context.Target.ServiceTypeId);
        if (serviceType is null)
        {
            return false;
        }

        context.Target.ReportAsCutType = serviceType.ReportAsCutType;

        return serviceType.ReportAsCutType is ReportAsCutTypes.Oil or ReportAsCutTypes.Water or ReportAsCutTypes.Solids;
    }
}
