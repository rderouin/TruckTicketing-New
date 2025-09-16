using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;

namespace SE.BillingService.Domain.InvoiceDelivery.Context;

public sealed class InvoiceDeliveryContext : IDisposable
{
    public Guid RequestId { get; set; }

    public DeliveryRequest Request { get; set; }

    public InvoiceExchangeEntity Config { get; set; }

    public InvoiceExchangeLookups Lookups { get; set; }

    public JObject Medium { get; set; }

    public EncodedInvoice EncodedInvoice { get; set; }

    public InvoiceExchangeDeliveryConfigurationEntity DeliveryConfig =>
        Request.GetMessageType() == MessageType.FieldTicketRequest
            ? Config?.FieldTicketsDeliveryConfiguration
            : Config?.InvoiceDeliveryConfiguration;

    public void Dispose()
    {
        EncodedInvoice?.Dispose();
    }

    public class InvoiceExchangeLookups
    {
        public IDictionary<Guid, SourceModelFieldEntity> SourceFields { get; set; }

        public IDictionary<Guid, DestinationModelFieldEntity> DestinationFields { get; set; }

        public IDictionary<Guid, ValueFormatEntity> ValueFormats { get; set; }
    }
}
