using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.Note;

using Trident.Contracts;
using Trident.Data.Contracts;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.SalesOrderAck)]
public class SalesOrderAckProcessor : BaseEntityProcessor<SalesOrderAckMessage>
{
    private readonly IProvider<Guid, InvoiceEntity> _invoiceProvider;

    private readonly ILogger<SalesOrderAckProcessor> _logger;

    private readonly IManager<Guid, NoteEntity> _noteManager;

    public SalesOrderAckProcessor(IProvider<Guid, InvoiceEntity> invoiceProvider, ILogger<SalesOrderAckProcessor> logger, IManager<Guid, NoteEntity> noteManager)
    {
        _invoiceProvider = invoiceProvider;
        _logger = logger;
        _noteManager = noteManager;
    }

    public override async Task Process(EntityEnvelopeModel<SalesOrderAckMessage> entityModel)
    {
        var ack = entityModel.Payload;
        var invoice = await _invoiceProvider.GetById(ack.Id); // PK - TODO: INT
        if (invoice is null)
        {
            _logger.LogWarning("Invoice {0} with ProformaInvoiceNumber {1} does not exist.", entityModel.Payload.Id, entityModel.Payload.ProformaInvoiceNumber);
            return;
        }

        if (invoice.IsDeliveredToErp.GetValueOrDefault(false))
        {
            _logger.LogInformation("Invoice {0} with ProformaInvoiceNumber {1} has already been delivered.", entityModel.Payload.Id, entityModel.Payload.ProformaInvoiceNumber);
            return;
        }

        var note = new NoteEntity
        {
            ThreadId = $"Invoice|{invoice.Id}",
            Comment = ack.IsSuccessful ? "Successful sales order delivery to FO" : $"Unsuccessful sales order delivery attempt to FO: {ack.Message}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "Integrations",
            CreatedBy = "Integrations",
        };

        await _noteManager.Save(note, true);

        invoice.IsDeliveredToErp = ack.IsSuccessful;
        await _invoiceProvider.Update(invoice);
    }
}

public class SalesOrderAckMessage
{
    public Guid Id { get; set; }

    public string ProformaInvoiceNumber { get; set; }

    public bool IsSuccessful { get; set; }

    public string Message { get; set; }
}
