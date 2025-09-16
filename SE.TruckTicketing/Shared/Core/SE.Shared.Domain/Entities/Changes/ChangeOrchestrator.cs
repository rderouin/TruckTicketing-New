using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

using Polly;

using SE.Shared.Common;
using SE.Shared.Common.Changes;
using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Threading;

using Trident.Contracts.Changes;
using Trident.Contracts.Configuration;
using Trident.Contracts.Extensions;
using Trident.Data;
using Trident.Data.Contracts;
using Trident.Domain;
using Trident.Extensions;
using Trident.Logging;

namespace SE.Shared.Domain.Entities.Changes;

public class ChangeOrchestrator : IChangeOrchestrator
{
    private const string ChangePersistenceRateKey = "ChangePersistenceRate";

    private const string ChangePersistenceSizeKey = "ChangePersistenceSize";

    private static readonly ConcurrentDictionary<string, ChangeConfiguration> Config = new();

    private static readonly ConcurrentDictionary<string, Type> TypeMap = new();

    private static readonly ResiliencePipeline<HttpStatusCode> ResiliencePipeline;

    private static readonly TimeSpan MaxLockDuration = TimeSpan.FromMinutes(1); // this value should be slightly over the total run of the 'ResiliencePipeline'

    private readonly IAppSettings _appSettings;

    private readonly IChangeService _changeService;

    private readonly Lazy<int> _lazyChangePersistenceRate;

    private readonly Lazy<int> _lazyChangePersistenceSize;

    private readonly Lazy<FeatureToggles> _lazyToggles;

    private readonly ILeaseObjectBlobStorage _leaseObjectBlobStorage;

    private readonly ILog _log;

    private readonly IDistributedRateLimiter _rateLimiter;

    private readonly IRepository<ChangeEntity> _repository;

    static ChangeOrchestrator()
    {
        // NOTE: total run for this pipeline is 55 sec
        ResiliencePipeline = new ResiliencePipelineBuilder<HttpStatusCode>().AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder<HttpStatusCode>().HandleResult(IsTransientCode)
                                                                 .Handle<CosmosException>(e => IsTransientCode(e.StatusCode)),
            MaxRetryAttempts = 10,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Linear,
            MaxDelay = TimeSpan.FromMinutes(1),
            UseJitter = true,
        }).Build();

        bool IsTransientCode(HttpStatusCode statusCode)
        {
            return statusCode is HttpStatusCode.TooManyRequests or
                                 HttpStatusCode.RequestTimeout or
                                 HttpStatusCode.ServiceUnavailable or
                                 HttpStatusCode.Gone or
                                 (HttpStatusCode)449;
        }
    }

    public ChangeOrchestrator(IChangeService changeService,
                              IRepository<ChangeEntity> repository,
                              IDistributedRateLimiter rateLimiter,
                              ILeaseObjectBlobStorage leaseObjectBlobStorage,
                              IAppSettings appSettings,
                              ILog log)
    {
        _changeService = changeService;
        _repository = repository;
        _rateLimiter = rateLimiter;
        _leaseObjectBlobStorage = leaseObjectBlobStorage;
        _appSettings = appSettings;
        _log = log;
        _lazyToggles = FeatureToggles.Init(appSettings);
        _lazyChangePersistenceRate = new(() => int.TryParse(appSettings.GetKeyOrDefault(ChangePersistenceRateKey), out var intValue) ? intValue : 6);
        _lazyChangePersistenceSize = new(() => int.TryParse(appSettings.GetKeyOrDefault(ChangePersistenceSizeKey), out var intValue) ? intValue : 20);
    }

    public async Task<bool> ProcessChange(ChangeModel changeModel)
    {
        // a toggle to disable the entire feature
        if (_lazyToggles.Value.DisableChangeTracking)
        {
            _log.Information(messageTemplate: "Change Tracking feature is disabled.");
            return false;
        }

        // load the configuration
        var config = Config.GetOrAdd(changeModel.ReferenceEntityType, t => ChangeConfiguration.Load(_appSettings, t));

        // load the type metadata
        var entityType = TypeMap.GetOrAdd(changeModel.ReferenceEntityType, GetActualType);

        // process the change entity
        return await ProcessChange(changeModel, config, entityType);
    }

    private Type GetActualType(string entityDiscriminator)
    {
        // fetch all entities
        var allTypes = typeof(ChangeEntity).Assembly.GetTypes();
        var allEntityTypes = allTypes.Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(Entity)));

        // find the matching Discriminator
        var entityType = allEntityTypes.FirstOrDefault(t => t.GetCustomAttribute<DiscriminatorAttribute>()?.Value == entityDiscriminator);

        // the entity type
        return entityType;
    }

    private async Task<bool> ProcessChange(ChangeModel changeModel, ChangeConfiguration config, Type type)
    {
        // compare objects
        var changes = _changeService.CompareObjects(type, changeModel.ObjectBefore, changeModel.ObjectAfter, config);

        // make the change entities
        var changeEntities = MakeEntities(changes, changeModel, config.TimeToLive, config.TagMap);

        // save the new entities
        return await SaveEntities(changeEntities);
    }

    private List<ChangeEntity> MakeEntities(List<FieldChange> changes, ChangeModel changeModel, double? timeToLive, Dictionary<string, string> tagMap)
    {
        var entities = new List<ChangeEntity>();

        // convert TTL into seconds, only positive real numbers are subject for the day conversion, leave special numbers as is ('null', '0', '-1')
        // -1 = infinite TTL
        // 0 = invalid TTL, convert to infinite
        // null = defer to the container setting
        var ttl = timeToLive > 0 ? (long?)Math.Ceiling(TimeSpan.FromDays(timeToLive.Value).TotalSeconds) : (long?)timeToLive;
        ttl = ttl == 0 ? -1 : ttl;

        // convert to entities
        foreach (var change in changes)
        {
            // create and populate
            var entity = new ChangeEntity
            {
                ReferenceEntityType = changeModel.ReferenceEntityType,
                ReferenceEntityId = changeModel.ReferenceEntityId,
                ReferenceEntityDocumentType = changeModel.ReferenceEntityDocumentType,
                FieldLocation = change.FieldLocation,
                FieldName = change.FieldName,
                ValueBefore = change.ValueBefore,
                ValueAfter = change.ValueAfter,
                ChangedById = changeModel.ChangedById,
                ChangedBy = changeModel.ChangedBy,
                ChangedAt = changeModel.ChangedAt,
                ChangeId = changeModel.ChangeId,
                FunctionName = changeModel.FunctionName,
                OperationId = changeModel.OperationId,
                TransactionId = changeModel.TransactionId,
                CorrelationId = changeModel.CorrelationId,
                TimeToLive = ttl,
            };

            // init
            entity.InitPrimaryKey();
            entity.InitPartitionKey();
            entity.InitAgnosticPath();

            // user tag
            entity.Tag = CreateTag(change, tagMap);

            // list of all entities
            entities.Add(entity);
        }

        return entities;

        static string CreateTag(FieldChange change, Dictionary<string, string> tagMap)
        {
            // ID to use
            var id = change.ObjectId;

            // if the map contains an alternative ID, then use it
            if (tagMap.TryGetValue(change.GetAgnosticPath(), out var fieldNameExpression))
            {
                // use the recorded token information to fetch the referenced field value
                id = change.GetReferenceFieldValue(fieldNameExpression);
            }

            // otherwise use a default approach to fallback to the referenced Object ID
            return Guid.TryParse(id, out var guid) ? guid.ToReferenceId() : null;
        }
    }

    private async Task<bool> SaveEntities(List<ChangeEntity> changeEntities)
    {
        // init
        var size = _lazyChangePersistenceSize.Value;
        var rate = _lazyChangePersistenceRate.Value;

        // save all entities directly to the Cosmos DB
        foreach (var partitionSpecificEntities in changeEntities.GroupBy(e => e.DocumentType))
        {
            // chunk the entities from a single partition
            var partitionKey = partitionSpecificEntities.Key;
            var chunkedEntities = partitionSpecificEntities.Chunk(size);

            // process each chunk with throttling and resiliency
            foreach (var entities in chunkedEntities)
            {
                // limit the rate at which entities go into the database
                await _rateLimiter.Execute(SaveEntitiesThrottled,
                                           _leaseObjectBlobStorage,
                                           $"{nameof(ChangeOrchestrator)}/lock",
                                           rate,
                                           MaxLockDuration);

                async Task<HttpStatusCode> SaveEntitiesThrottled()
                {
                    // any time the entities are saved, make sure the save goes through the resilience pipeline to ensure durability
                    var statusCode = await ResiliencePipeline.ExecuteAsync(async token => await _repository.SaveNativeAsync<ChangeEntity, string>(entities, partitionKey, token));

                    // fail-early on failed requests that span beyond the resiliency boundaries
                    if (!statusCode.IsSuccessStatusCode())
                    {
                        var firstSample = partitionSpecificEntities.FirstOrDefault()?.ToJson();
                        var message = $"Unable to save Change entities. Cosmos DB returned this status code: {(int)statusCode} ({statusCode}). First sample:{Environment.NewLine}{firstSample}";
                        _log.Error(messageTemplate: message);
                        throw new InvalidOperationException(message);
                    }

                    return statusCode;
                }
            }
        }

        return true;
    }
}
