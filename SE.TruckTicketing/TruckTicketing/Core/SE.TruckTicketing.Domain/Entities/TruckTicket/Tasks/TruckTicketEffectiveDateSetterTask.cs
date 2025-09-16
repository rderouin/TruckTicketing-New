using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public interface ITruckTicketEffectiveDateService
{
    Task<DateTime?> GetTruckTicketEffectiveDate(TruckTicketEntity truckTicket);
}

public class TruckTicketEffectiveDateSetterTask : WorkflowTaskBase<BusinessContext<TruckTicketEntity>>, ITruckTicketEffectiveDateService
{
    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    public TruckTicketEffectiveDateSetterTask(IProvider<Guid, FacilityEntity> facilityProvider)
    {
        _facilityProvider = facilityProvider;
    }

    public override int RunOrder => 1;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public async Task<DateTime?> GetTruckTicketEffectiveDate(TruckTicketEntity ticket)
    {
        return ticket.TruckTicketType switch
               {
                   TruckTicketType.WT => CurrentDateStrategy(ticket),
                   TruckTicketType.SP => await TMinusOneDateStrategy(ticket),
                   TruckTicketType.LF => await TMinusOneDateStrategy(ticket),
                   _ => null,
               };
    }

    public override async Task<bool> Run(BusinessContext<TruckTicketEntity> context)
    {
        var ticket = context.Target;

        ticket.EffectiveDate = await GetTruckTicketEffectiveDate(ticket);

        return true;
    }

    private static DateTime? CurrentDateStrategy(TruckTicketEntity truckTicket)
    {
        return truckTicket.LoadDate?.Date;
    }

    private async Task<DateTime?> TMinusOneDateStrategy(TruckTicketEntity truckTicket)
    {
        var operatingDayCutOffTime = await GetOperatingDayCutOffTime(truckTicket);
        var ticketTimeOut = GetTruckTicketTimeOut(truckTicket) ?? operatingDayCutOffTime;

        var effectiveDate = CurrentDateStrategy(truckTicket);
        if (ticketTimeOut < operatingDayCutOffTime)
        {
            effectiveDate = effectiveDate?.AddDays(-1);
        }

        return effectiveDate;
    }

    private async Task<TimeOnly> GetOperatingDayCutOffTime(TruckTicketEntity ticket)
    {
        var facility = await _facilityProvider.GetById(ticket.FacilityId);
        var cutOffTime = facility?.OperatingDayCutOffTime ?? new(DateTime.Today.AddHours(7));
        return TimeOnly.FromDateTime(cutOffTime.DateTime);
    }

    private static TimeOnly? GetTruckTicketTimeOut(TruckTicketEntity ticket)
    {
        return ticket.TimeOut.HasValue ? TimeOnly.FromDateTime(ticket.TimeOut.Value.DateTime) : null;
    }

    public override Task<bool> ShouldRun(BusinessContext<TruckTicketEntity> context)
    {
        var original = context.Original;
        var target = context.Target;

        var shouldRun = original?.LoadDate != target.LoadDate ||
                        original?.TimeOut != target.TimeOut ||
                        (target.EffectiveDate is null && target.LoadDate.HasValue && target.TimeOut.HasValue);

        return Task.FromResult(shouldRun);
    }
}
