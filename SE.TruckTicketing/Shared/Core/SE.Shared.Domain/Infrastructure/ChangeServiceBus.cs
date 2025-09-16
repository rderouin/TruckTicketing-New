using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.Changes;
using SE.TridentContrib.Extensions.Azure.ServiceBus;

using Trident.Contracts.Changes;
using Trident.Contracts.Configuration;

namespace SE.Shared.Domain.Infrastructure;

public class ChangeServiceBus : ServiceBus, IChangeServiceBus
{
    private readonly IAppSettings _appSettings;

    public ChangeServiceBus(string connectionString, IAppSettings appSettings) : base(connectionString)
    {
        _appSettings = appSettings;
    }

    public async Task EnqueueChange(EntityEnvelopeModel<ChangeModel> change, CancellationToken cancellationToken = default)
    {
        await Enqueue(GetTopicName(), change, GetMetadata(change), default, cancellationToken);
    }

    private string GetTopicName()
    {
        return _appSettings.GetKeyOrDefault(ServiceBusConstants.Topics.ChangeEntities.Trim('%'), "change-entities");
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
