using System.Threading;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.TridentContrib.Extensions.Azure.ServiceBus;

using Trident.Contracts.Changes;

namespace SE.Shared.Domain.Entities.Changes;

public interface IChangeServiceBus : IServiceBus
{
    Task EnqueueChange(EntityEnvelopeModel<ChangeModel> change, CancellationToken cancellationToken = default);
}
