using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Tasks;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Contracts.Api;
using Trident.Contracts.Configuration;
using Trident.Data.Contracts;

namespace SE.Shared.Domain.Entities.Invoices;

public class SalesOrderPublisher : ISalesOrderPublisher
{
    private readonly IAppSettings _appSettings;

    private readonly IEntityPublisher _entityPublisher;

    private readonly IEntityPublishMessageTask<InvoiceEntity> _entityPublishMessageTask;

    private readonly IProvider<Guid, InvoiceEntity> _invoiceProvider;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    private readonly ISalesLinesPublisher _salesLinesPublisher;

    public SalesOrderPublisher(IEntityPublisher entityPublisher,
                               ISalesLinesPublisher salesLinesPublisher,
                               IProvider<Guid, InvoiceEntity> invoiceProvider,
                               IProvider<Guid, SalesLineEntity> salesLineProvider,
                               IAppSettings appSettings,
                               IEntityPublishMessageTask<InvoiceEntity> entityPublishMessageTask)
    {
        _entityPublisher = entityPublisher;
        _salesLinesPublisher = salesLinesPublisher;
        _invoiceProvider = invoiceProvider;
        _salesLineProvider = salesLineProvider;
        _appSettings = appSettings;
        _entityPublishMessageTask = entityPublishMessageTask;
    }

    public async Task PublishSalesOrder(CompositeKey<Guid> invoiceKey)
    {
        var invoice = await _invoiceProvider.GetById(invoiceKey); // PK - OK
        if (invoice?.Status is not InvoiceStatus.UnPosted)
        {
            return;
        }

        await EnqueueSalesOrderMessage(new(invoice, invoice) { Operation = Operation.Update });

        var salesLines = (await _salesLineProvider.Get(salesLine => salesLine.InvoiceId == invoiceKey.Id))?.ToList() ?? new(); // PK - XP for SL by Invoice ID
        if (salesLines.Any())
        {
            int.TryParse(_appSettings.GetKeyOrDefault("SalesLinePublishBatchSize", "100"), out var batchSize);
            foreach (var salesLineBatch in salesLines.Chunk(batchSize))
            {
                await _salesLinesPublisher.PublishSalesLines(salesLineBatch);
            }
        }
    }

    private async Task EnqueueSalesOrderMessage(BusinessContext<InvoiceEntity> context)
    {
        string sessionId = default;
        var targetEntity = context.Target;
        Func<EntityEnvelopeModel<InvoiceEntity>, Task> envelopeEnricher = _ => Task.CompletedTask;
        if (_entityPublishMessageTask != null)
        {
            sessionId = await _entityPublishMessageTask.GetSessionIdForMessage(context);
            targetEntity = await _entityPublishMessageTask.EvaluateEntityForUpdates(context);
            envelopeEnricher = _entityPublishMessageTask.EnrichEnvelopeModel;
        }

        await _entityPublisher.EnqueueMessage(targetEntity, context.Operation.ToString(), sessionId, envelopeEnricher);
    }
}

public interface ISalesOrderPublisher
{
    Task PublishSalesOrder(CompositeKey<Guid> invoiceKey);
}
