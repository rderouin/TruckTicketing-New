using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.InvoiceDelivery;
using SE.TridentContrib.Extensions.Azure.ServiceBus;

using Trident.Contracts.Configuration;

namespace SE.Shared.Domain.Infrastructure;

public class InvoiceDeliveryServiceBus : ServiceBus, IInvoiceDeliveryServiceBus
{
    private readonly IAppSettings _appSettings;

    public InvoiceDeliveryServiceBus(string connectionString, IAppSettings appSettings) : base(connectionString)
    {
        _appSettings = appSettings;
    }

    public async Task EnqueueRequest<T>(EntityEnvelopeModel<T> request, CancellationToken cancellationToken = default)
    {
        await Enqueue(GetTopicName(), request, GetMetadata(request), default, cancellationToken);
    }

    public async Task EnqueueResponse<T>(EntityEnvelopeModel<T> response, CancellationToken cancellationToken = default)
    {
        await Enqueue(GetTopicName(), response, GetMetadata(response), default, cancellationToken);
    }

    private string GetTopicName()
    {
        return _appSettings.GetKeyOrDefault("Topic:InvoiceDelivery", "invoice-delivery");
    }

    private Dictionary<string, string> GetMetadata<T>(EntityEnvelopeModel<T> model)
    {
        return new()
        {
            [nameof(model.Source)] = model.Source,
            [nameof(model.MessageType)] = model.MessageType,
            [nameof(model.CorrelationId)] = model.CorrelationId,
            [nameof(model.Operation)] = model.Operation,
        };
    }
}
