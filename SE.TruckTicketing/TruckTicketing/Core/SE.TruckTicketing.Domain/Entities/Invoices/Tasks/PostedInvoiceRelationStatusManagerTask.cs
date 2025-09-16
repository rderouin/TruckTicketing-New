using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.Invoices.Tasks;

public class PostedInvoiceRelationStatusManagerTask : WorkflowTaskBase<BusinessContext<InvoiceEntity>>
{
    private readonly IProvider<Guid, LoadConfirmationEntity> _loadConfirmationProvider;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    public PostedInvoiceRelationStatusManagerTask(IProvider<Guid, LoadConfirmationEntity> loadConfirmationProvider,
                                                  IProvider<Guid, SalesLineEntity> salesLineProvider,
                                                  IProvider<Guid, TruckTicketEntity> truckTicketProvider)
    {
        _loadConfirmationProvider = loadConfirmationProvider;
        _salesLineProvider = salesLineProvider;
        _truckTicketProvider = truckTicketProvider;
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<InvoiceEntity> context)
    {
        await UpdateLoadConfirmationStatuses(context.Target);
        var truckTicketIds = await UpdateSalesLineStatuses(context.Target.Id);
        await UpdateTruckTicketStatuses(truckTicketIds);
        return true;
    }

    private async Task UpdateLoadConfirmationStatuses(InvoiceEntity invoice)
    {
        var loadConfirmations = (await _loadConfirmationProvider.Get(lc => lc.InvoiceId == invoice.Id)).ToList(); // PK - XP for LC by Invoice ID
        foreach (var loadConfirmation in loadConfirmations)
        {
            if (invoice.Status is InvoiceStatus.Posted && loadConfirmation.Status is not LoadConfirmationStatus.Posted)
            {
                loadConfirmation.Status = LoadConfirmationStatus.Posted;
            }

            loadConfirmation.InvoiceStatus = invoice.Status;
            await _loadConfirmationProvider.Update(loadConfirmation, true);
        }
    }

    private async Task<IEnumerable<Guid>> UpdateSalesLineStatuses(Guid invoiceId)
    {
        var truckTicketIds = new HashSet<Guid>();
        var salesLines = (await _salesLineProvider.Get(sl => sl.InvoiceId == invoiceId)).ToList(); // PK - XP for SL by Invoice ID 
        foreach (var salesLine in salesLines)
        {
            salesLine.Status = SalesLineStatus.Posted;
            truckTicketIds.Add(salesLine.TruckTicketId);
            await _salesLineProvider.Update(salesLine, true);
        }

        return truckTicketIds;
    }

    private async Task UpdateTruckTicketStatuses(IEnumerable<Guid> truckTicketIds)
    {
        var truckTickets = await _truckTicketProvider.GetByIds(truckTicketIds); // PK - TODO: ENTITY or INDEX
        foreach (var truckTicket in truckTickets)
        {
            truckTicket.Status = TruckTicketStatus.Invoiced;
            await _truckTicketProvider.Update(truckTicket, true);
        }
    }

    public override Task<bool> ShouldRun(BusinessContext<InvoiceEntity> context)
    {
        var invoice = context.Target;
        var shouldRun = context.Original?.Status is InvoiceStatus.UnPosted or InvoiceStatus.AgingUnSent &&
                        invoice.Status is InvoiceStatus.Posted or InvoiceStatus.AgingUnSent &&
                        context.Original?.Status != invoice.Status;

        return Task.FromResult(shouldRun);
    }
}
