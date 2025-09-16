using System.Threading;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.TridentContrib.Extensions.Azure.ServiceBus;

namespace SE.Shared.Domain.Entities.InvoiceDelivery;

public interface IInvoiceDeliveryServiceBus : IServiceBus
{
    Task EnqueueRequest<T>(EntityEnvelopeModel<T> request, CancellationToken cancellationToken = default);

    Task EnqueueResponse<T>(EntityEnvelopeModel<T> response, CancellationToken cancellationToken = default);
}
