using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Infrastructure;

using Trident.Contracts.Configuration;

namespace SE.Shared.Domain;

public class EntityPublisher : IEntityPublisher
{
    public EntityPublisher(IIntegrationsServiceBus integrationsServiceBus, IAppSettings appSettings)
    {
        _integrationsServiceBus = integrationsServiceBus;
        TopicName = new(() => appSettings.GetKeyOrDefault(ServiceBusConstants.Topics.EntityUpdates.Trim('%'), "enterprise-entity-updates"));
    }

    private Lazy<string> TopicName { get; }

    private IIntegrationsServiceBus _integrationsServiceBus { get; }

    public async Task EnqueueMessage<T>(EntityEnvelopeModel<T> envelope, string sessionId, Func<EntityEnvelopeModel<T>, Task> enrichEnvelopeModel = null)
    {
        if (enrichEnvelopeModel is not null)
        {
            await enrichEnvelopeModel(envelope);
        }

        var metadata = new Dictionary<string, string>
        {
            [nameof(envelope.Source)] = envelope.Source,
            [nameof(envelope.MessageType)] = envelope.MessageType,
        };

        await _integrationsServiceBus.Enqueue(TopicName.Value, envelope, metadata, sessionId);
    }

    public async Task EnqueueMessage<T>(T entity, string operation, string sessionId, Func<EntityEnvelopeModel<T>, Task> enrichEnvelopeModel = null) where T : TTEntityBase
    {
        var envelope = new EntityEnvelopeModel<T>
        {
            CorrelationId = Guid.NewGuid().ToString(),
            EnterpriseId = entity.Id,
            SourceId = entity.Id.ToString(),
            MessageDate = DateTime.UtcNow,
            MessageType = entity.EntityType,
            Source = ServiceBusConstants.Sources.TruckTicketing,
            Payload = entity,
            Operation = operation,
        };

        if (enrichEnvelopeModel is not null)
        {
            await enrichEnvelopeModel(envelope);
        }

        var metadata = new Dictionary<string, string>
        {
            [nameof(envelope.Source)] = envelope.Source,
            [nameof(envelope.MessageType)] = envelope.MessageType,
        };

        await _integrationsServiceBus.Enqueue(TopicName.Value, envelope, metadata, sessionId);
    }

    public async Task EnqueueBulkMessage<T>(IList<T> entities, string operation, string sessionId) where T : TTEntityBase
    {
        if (!entities.Any())
        {
            return;
        }

        var envelope = new EntityEnvelopeModel<IList<T>>
        {
            CorrelationId = Guid.NewGuid().ToString(),
            EnterpriseId = entities.First().Id,
            MessageDate = DateTime.UtcNow,
            MessageType = entities.First().EntityType,
            Source = ServiceBusConstants.Sources.TruckTicketing,
            Payload = entities,
            Operation = operation,
        };

        var metadata = new Dictionary<string, string>
        {
            [nameof(envelope.Source)] = envelope.Source,
            [nameof(envelope.MessageType)] = envelope.MessageType,
        };

        await _integrationsServiceBus.Enqueue(TopicName.Value, envelope, metadata, sessionId);
    }
}

public interface IEntityPublisher
{
    Task EnqueueMessage<T>(EntityEnvelopeModel<T> envelope, string sessionId = null, Func<EntityEnvelopeModel<T>, Task> enrichEnvelopeModel = null);

    Task EnqueueMessage<T>(T entity, string operation, string sessionId, Func<EntityEnvelopeModel<T>, Task> enrichEnvelopeModel = null) where T : TTEntityBase;

    Task EnqueueBulkMessage<T>(IList<T> entities, string operation, string sessionId) where T : TTEntityBase;
}
