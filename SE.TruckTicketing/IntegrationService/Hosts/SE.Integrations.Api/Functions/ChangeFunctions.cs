using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Messaging.ServiceBus;

using Microsoft.Azure.Functions.Worker;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common;
using SE.Shared.Common.Changes;
using SE.Shared.Domain.Entities.Changes;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Threading;

using Trident.Contracts.Changes;
using Trident.Contracts.Configuration;
using Trident.Extensions;
using Trident.Logging;

namespace SE.Integrations.Api.Functions;

public class ChangeFunctions
{
    private const string ChangeProcessorRateKey = "ChangeProcessorRate";

    private static readonly ConcurrentDictionary<string, ChangeConfiguration> Config = new();

    private readonly IAppSettings _appSettings;

    private readonly IChangeBlobStorage _changeBlobStorage;

    private readonly IChangeOrchestrator _changeOrchestrator;

    private readonly Lazy<FeatureToggles> _lazyToggles;

    private readonly ILeaseObjectBlobStorage _leaseObjectBlobStorage;

    private readonly ILog _log;

    private readonly IDistributedRateLimiter _rateLimiter;

    public ChangeFunctions(IChangeOrchestrator changeOrchestrator,
                           IChangeBlobStorage changeBlobStorage,
                           ILeaseObjectBlobStorage leaseObjectBlobStorage,
                           IDistributedRateLimiter rateLimiter,
                           IAppSettings appSettings,
                           ILog log)
    {
        _changeOrchestrator = changeOrchestrator;
        _changeBlobStorage = changeBlobStorage;
        _leaseObjectBlobStorage = leaseObjectBlobStorage;
        _rateLimiter = rateLimiter;
        _appSettings = appSettings;
        _log = log;

        _lazyToggles = FeatureToggles.Init(appSettings);
    }

    [Function(nameof(ProcessChange))]
    public async Task ProcessChange([ServiceBusTrigger(ServiceBusConstants.Topics.ChangeEntities,
                                                       ServiceBusConstants.Subscriptions.ChangeProcess,
                                                       Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
                                    string message,
                                    FunctionContext context)
    {
        // is this feature disabled?
        if (_lazyToggles.Value.DisableChangeTracking)
        {
            return;
        }

        // init
        ChangeModel changeModel = null;
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            // general null-check
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new InvalidOperationException($"The message is blank while executing the {nameof(ProcessChange)} function.");
            }

            // retrieve the envelope model
            var envelopeModel = JsonConvert.DeserializeObject<EntityEnvelopeModel<ChangeModel>>(message);

            // init telemetry metadata based on the provided message
            changeModel = envelopeModel?.Payload ?? throw new InvalidOperationException($"The message does not have the payload:{Environment.NewLine}{message}");

            // load the configuration for the deserialized entity
            var config = Config.GetOrAdd(changeModel.ReferenceEntityType, t => ChangeConfiguration.Load(_appSettings, t));

            // the configuration might be blank for the given entity, if so, skip comparing objects
            if (!config.HasFieldsToTrack())
            {
                // telemetry: skipped message due to config
                LogEvent("Ignored");
                return;
            }

            // fetch the configured rate value for the limiter
            var changeProcessorRateValue = _appSettings.GetKeyOrDefault(ChangeProcessorRateKey);
            var slots = int.TryParse(changeProcessorRateValue, out var s) ? s : 20; // hard fallback value

            // process the change via the distributed rate limiter, the rate is global and depends upon the container size allocation
            await _rateLimiter.Execute(async () => await _changeOrchestrator.ProcessChange(envelopeModel.Payload),
                                       _leaseObjectBlobStorage,
                                       $"{nameof(ProcessChange)}/lock", // a global lock for this function, the rate limiter will append a slot number
                                       slots, // the ideal value is inherited by the performance of the Cosmos DB container
                                       TimeSpan.FromMinutes(5));

            // telemetry: successfully processed message
            LogEvent("Successful");
        }
        catch (Exception e)
        {
            // log the exception
            _log.Error(exception: e, messageTemplate: $"Unable to process the change:{Environment.NewLine}{message}");

            // telemetry: failed to process the message
            LogEvent("Failed");

            throw;
        }

        void LogEvent(string status)
        {
            var elapsed = DateTimeOffset.UtcNow - startTime;
            _log.Information(messageTemplate: new Dictionary<string, string>
            {
                ["EventName"] = nameof(ProcessChange),
                [nameof(changeModel.ReferenceEntityType)] = changeModel?.ReferenceEntityType,
                [nameof(changeModel.ReferenceEntityId)] = changeModel?.ReferenceEntityId,
                [nameof(changeModel.ReferenceEntityDocumentType)] = changeModel?.ReferenceEntityDocumentType,
                [nameof(changeModel.FunctionName)] = changeModel?.FunctionName,
                [nameof(changeModel.ChangedBy)] = changeModel?.ChangedBy,
                [nameof(changeModel.ChangedById)] = changeModel?.ChangedById,
                [nameof(elapsed.Duration)] = elapsed.ToString(),
                ["Status"] = status,
            }.ToJson());
        }
    }

    [Function(nameof(ArchiveChange))]
    public async Task ArchiveChange([ServiceBusTrigger(ServiceBusConstants.Topics.ChangeEntities,
                                                       ServiceBusConstants.Subscriptions.ChangeArchive,
                                                       Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
                                    byte[] data,
                                    FunctionContext context)
    {
        // is this feature disabled?
        if (_lazyToggles.Value.DisableChangeTracking)
        {
            return;
        }

        string message = null;

        try
        {
            // general null-check
            if (data == null)
            {
                throw new InvalidOperationException($"The message is blank while executing the {nameof(ArchiveChange)} function.");
            }

            // is this a recovery mode?
            var isRecovery = IsRecoveryMode(context.BindingContext);
            if (isRecovery)
            {
                return;
            }

            // retrieve the envelope model
            message = Encoding.UTF8.GetString(data);
            var envelopeModel = JsonConvert.DeserializeObject<EntityEnvelopeModel<ChangeModel>>(message);

            // save the message into a stream
            await using var stream = new MemoryStream(data);

            // process the change
            var containerName = _changeBlobStorage.DefaultContainerName;
            var blobName = GetBlobName(envelopeModel.Payload);
            await _changeBlobStorage.Upload(containerName, blobName, stream);
            await _changeBlobStorage.SetMetadata(containerName, blobName, CleanDictionary(GetMetadata(envelopeModel.Payload)));
            await _changeBlobStorage.SetTags(containerName, blobName, CleanDictionary(GetTags(envelopeModel.Payload)));
        }
        catch (Exception e)
        {
            _log.Error(exception: e, messageTemplate: $"Unable to archive the change:{Environment.NewLine}{message}");
            throw;
        }

        static string GetBlobName(ChangeModel changeModel)
        {
            var utcDateTime = changeModel.ChangedAt.UtcDateTime; // point-in-time for chronological order
            var utcDate = DateOnly.FromDateTime(utcDateTime); // date-only component of the point-in-time
            var utcTime = TimeOnly.FromDateTime(utcDateTime); // time-only component of the point-in-time
            var operationId = changeModel.OperationId; // a grouping ID for all changes for that time and user
            var entityType = changeModel.ReferenceEntityType; // entity that was changed
            var entityId = changeModel.ReferenceEntityId; // ID of the entity that was changed
            var changeId = changeModel.ChangeId; // a unique ID of the change

            // this represents one single entity change sorted chronologically
            return $"{entityType}/{utcDate:O}/{utcTime:O}/{entityId}/{operationId}/{changeId}.json";
        }

        static Dictionary<string, string> GetMetadata(ChangeModel changeModel)
        {
            return new()
            {
                [nameof(changeModel.ReferenceEntityType)] = changeModel.ReferenceEntityType,
                [nameof(changeModel.ReferenceEntityId)] = changeModel.ReferenceEntityId,
                [nameof(changeModel.ReferenceEntityDocumentType)] = changeModel.ReferenceEntityDocumentType,
                [nameof(changeModel.ChangedBy)] = changeModel.ChangedBy,
                [nameof(changeModel.ChangedById)] = changeModel.ChangedById,
                [nameof(changeModel.ChangedAt)] = $"{changeModel.ChangedAt:O}",
                [nameof(changeModel.ChangeId)] = $"{changeModel.ChangeId}",
                [nameof(changeModel.FunctionName)] = changeModel.FunctionName,
                [nameof(changeModel.OperationId)] = $"{changeModel.OperationId}",
                [nameof(changeModel.TransactionId)] = changeModel.TransactionId,
                [nameof(changeModel.CorrelationId)] = changeModel.CorrelationId,
            };
        }

        static Dictionary<string, string> GetTags(ChangeModel changeModel)
        {
            return new()
            {
                [nameof(changeModel.ReferenceEntityType)] = changeModel.ReferenceEntityType,
                [nameof(changeModel.ReferenceEntityId)] = changeModel.ReferenceEntityId,
                [nameof(changeModel.ChangedAt)] = $"{changeModel.ChangedAt:O}",
                [nameof(changeModel.ChangeId)] = $"{changeModel.ChangeId}",
                [nameof(changeModel.FunctionName)] = changeModel.FunctionName,
                [nameof(changeModel.OperationId)] = $"{changeModel.OperationId}",
                [nameof(changeModel.TransactionId)] = changeModel.TransactionId,
                [nameof(changeModel.CorrelationId)] = changeModel.CorrelationId,
            };
        }

        static Dictionary<string, string> CleanDictionary(Dictionary<string, string> dictionary)
        {
            foreach (var pair in dictionary.ToList())
            {
                if (string.IsNullOrWhiteSpace(dictionary[pair.Key]))
                {
                    dictionary.Remove(pair.Key);
                }
            }

            return dictionary;
        }

        bool IsRecoveryMode(BindingContext bindingContext)
        {
            // fetch the application properties from metadata
            var propertiesString = bindingContext.BindingData.GetValueOrDefault(nameof(ServiceBusMessage.ApplicationProperties)) as string;
            if (propertiesString == null)
            {
                return false;
            }

            // the original dictionary is stored as a JSON serialized string
            var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(propertiesString);
            if (properties == null)
            {
                return false;
            }

            // if the message is tagged with this metadata property, skip storing/processing, this is the data recovery process
            return properties.ContainsKey(MessageConstants.Changes.Recovery);
        }
    }
}
