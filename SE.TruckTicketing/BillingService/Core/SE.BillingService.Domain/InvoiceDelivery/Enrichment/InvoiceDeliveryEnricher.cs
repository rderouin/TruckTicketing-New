using System;
using System.Linq;
using System.Threading.Tasks;

using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.Shared.Domain.Infrastructure;

using Trident.Contracts;

namespace SE.BillingService.Domain.InvoiceDelivery.Enrichment;

public class InvoiceDeliveryEnricher : IInvoiceDeliveryEnricher
{
    private readonly IInvoiceAttachmentsBlobStorage _blobStorage;

    private readonly IManager<Guid, DestinationModelFieldEntity> _destinationFieldManager;

    private readonly IManager<Guid, SourceModelFieldEntity> _sourceFieldManager;

    private readonly IManager<Guid, ValueFormatEntity> _valueFormatManager;

    public InvoiceDeliveryEnricher(IManager<Guid, SourceModelFieldEntity> sourceFieldManager,
                                   IManager<Guid, DestinationModelFieldEntity> destinationFieldManager,
                                   IManager<Guid, ValueFormatEntity> valueFormatManager,
                                   IInvoiceAttachmentsBlobStorage blobStorage)
    {
        _sourceFieldManager = sourceFieldManager;
        _destinationFieldManager = destinationFieldManager;
        _valueFormatManager = valueFormatManager;
        _blobStorage = blobStorage;
    }

    public async Task Enrich(InvoiceDeliveryContext context)
    {
        // get IDs
        var sourceIds = context.DeliveryConfig.Mappings?
                               .Where(e => e.SourceModelFieldId != null)
                               .Select(e => e.SourceModelFieldId.Value)
                               .ToHashSet();

        var destinationIds = context.DeliveryConfig.Mappings?
                                    .Where(e => e.DestinationModelFieldId != null)
                                    .Select(e => e.DestinationModelFieldId.Value)
                                    .ToHashSet();

        var formatIds = context.DeliveryConfig.Mappings?
                               .Where(e => e.DestinationFormatId != null)
                               .Select(e => e.DestinationFormatId.Value)
                               .ToHashSet();

        // init lookups
        context.Lookups = new()
        {
            // source fields lookup
            SourceFields = sourceIds?.Any() == true
                               ? (await _sourceFieldManager.GetByIds(sourceIds)).ToDictionary(e => e.Id, e => e)
                               : new(),

            // destination fields lookup
            DestinationFields = destinationIds?.Any() == true
                                    ? (await _destinationFieldManager.GetByIds(destinationIds)).ToDictionary(e => e.Id, e => e)
                                    : new(),

            // formats lookup
            ValueFormats = formatIds?.Any() == true
                               ? (await _valueFormatManager.GetByIds(formatIds)).ToDictionary(e => e.Id, e => e)
                               : new(),
        };
    }
}
