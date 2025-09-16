using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.BillingConfigurations.Tasks;

public class BillingConfigEdiReferenceUpdaterTask : WorkflowTaskBase<BusinessContext<BillingConfigurationEntity>>
{
    private readonly IManager<Guid, InvoiceEntity> _invoiceManager;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    private readonly ISalesLinesPublisher _salesLinesPublisher;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    public BillingConfigEdiReferenceUpdaterTask(IProvider<Guid, TruckTicketEntity> truckTicketProvider,
                                                IProvider<Guid, SalesLineEntity> salesLineProvider,
                                                IManager<Guid, InvoiceEntity> invoiceManager,
                                                ISalesLinesPublisher salesLinesPublisher)
    {
        _truckTicketProvider = truckTicketProvider;
        _salesLineProvider = salesLineProvider;
        _invoiceManager = invoiceManager;
        _salesLinesPublisher = salesLinesPublisher;
    }

    public override int RunOrder => 100;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<BillingConfigurationEntity> context)
    {
        var config = context.Target;
        var truckTicketIdsToUpdate = await UpdateUnsentSalesLineEdi(config);
        if (truckTicketIdsToUpdate.Count > 0)
        {
            await UpdateTruckTicketEdi(truckTicketIdsToUpdate, config);
        }

        return true;
    }

    private async Task<ICollection<Guid>> UpdateUnsentSalesLineEdi(BillingConfigurationEntity config)
    {
        var invoices = (await _invoiceManager.Get(invoice => (invoice.Status == InvoiceStatus.UnPosted || invoice.Status == InvoiceStatus.AgingUnSent) &&
                                                             invoice.BillingConfigurationId == config.Id))
                      .DistinctBy(invoice => invoice.Id).ToDictionary(invoice => invoice.Id); // PK - XP for invoices by billing config

        if (invoices.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var invoiceIds = invoices.Keys.Cast<Guid?>().ToList();

        var salesLines = (await _salesLineProvider.Get(sl => !sl.IsReversed &&
                                                             !sl.IsReversal &&
                                                             invoiceIds.Contains(sl.InvoiceId))).ToList(); // PK - XP for SL by Invoice ID

        if (salesLines.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var truckTicketsToUpdate = new HashSet<Guid>();

        foreach (var salesLineGroup in salesLines.GroupBy(salesLine => salesLine.InvoiceId))
        {
            var lines = salesLineGroup.ToArray();
            
            foreach (var salesLine in lines)
            {
                truckTicketsToUpdate.Add(salesLine.TruckTicketId);
                salesLine.EdiFieldValues = config.EDIValueData.Clone();
                await _salesLineProvider.Update(salesLine, true);
            }

            await _salesLinesPublisher.PublishSalesLines(lines);

            if (invoices.TryGetValue(salesLineGroup.Key.GetValueOrDefault(), out var invoice))
            {
                invoice.RequiresPdfRegeneration = true;
                await _invoiceManager.Save(invoice, true);
            }
        }

        return truckTicketsToUpdate;
    }

    private async Task UpdateTruckTicketEdi(ICollection<Guid> truckTicketIds, BillingConfigurationEntity config)
    {
        var truckTickets = (await _truckTicketProvider.Get(tt => truckTicketIds.Contains(tt.Id) &&
                                                                 tt.Status != TruckTicketStatus.Void)).ToList(); // PK - TODO: ENTITY or INDEX

        foreach (var truckTicket in truckTickets)
        {
            truckTicket.EdiFieldValues = config.EDIValueData.Clone();
            await _truckTicketProvider.Update(truckTicket, true);
        }
    }

    public override Task<bool> ShouldRun(BusinessContext<BillingConfigurationEntity> context)
    {
        if (context.Operation != Operation.Update)
        {
            return Task.FromResult(false);
        }

        var originalEdiValues = context.Original.EDIValueData ?? new();
        var targetEdiValues = context.Target.EDIValueData ?? new();

        var shouldRun = !targetEdiValues.OrderBy(e => e.EDIFieldDefinitionId)
                                        .SequenceEqual(originalEdiValues.OrderBy(e => e.EDIFieldDefinitionId),
                                                       new EdiValueEqualityComparer());

        return Task.FromResult(shouldRun);
    }

    private sealed class EdiValueEqualityComparer : IEqualityComparer<EDIFieldValueEntity>
    {
        public bool Equals(EDIFieldValueEntity x, EDIFieldValueEntity y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null))
            {
                return false;
            }

            if (ReferenceEquals(y, null))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.EDIFieldDefinitionId.Equals(y.EDIFieldDefinitionId) && x.EDIFieldValueContent == y.EDIFieldValueContent;
        }

        public int GetHashCode(EDIFieldValueEntity obj)
        {
            return HashCode.Combine(obj.EDIFieldDefinitionId, obj.EDIFieldValueContent);
        }
    }
}
