using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;

using Trident.Contracts.Changes;

namespace SE.Shared.Domain.Entities.Changes;

public class ChangePublisher : IChangePublisher
{
    private readonly IChangeServiceBus _serviceBus;

    public ChangePublisher(IChangeServiceBus serviceBus)
    {
        _serviceBus = serviceBus;
    }

    public async Task Publish(ChangeModel changeModel)
    {
        // create a message
        var envelope = new EntityEnvelopeModel<ChangeModel>
        {
            Payload = changeModel,
            Blobs = new(),
            MessageDate = DateTime.UtcNow,
            MessageType = nameof(ChangeModel),
            Operation = "Change",
            Source = "TT",
            EnterpriseId = changeModel.ChangeId, // same as SourceId
            CorrelationId = Guid.NewGuid().ToString(),
        };

        // publish the message to the service bus
        await _serviceBus.EnqueueChange(envelope);
    }
}
