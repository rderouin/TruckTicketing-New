using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SE.Shared.Common;
using SE.TridentContrib.Extensions.Azure.Functions;
using SE.TridentContrib.Extensions.Security;

using Trident.Contracts.Api;
using Trident.Contracts.Changes;
using Trident.Contracts.Configuration;
using Trident.Domain;
using Trident.EFCore.Changes;
using Trident.Extensions;
using Trident.Logging;

namespace SE.Shared.Domain.Entities.Changes;

public class ChangeObserver : IChangeObserver
{
    private const string FieldNameEntityType = nameof(DocumentDbEntityBase<object>.EntityType);

    private const string FieldNameId = nameof(DocumentDbEntityBase<object>.Id);

    private const string FieldNameDocumentType = nameof(DocumentDbEntityBase<object>.DocumentType);

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Converters = new List<JsonConverter>
        {
            new StringHumanizedEnumConverter(),
        },
    };

    private static readonly HashSet<string> NonTrackedTypes = new()
    {
        typeof(CompositeKey<object>).Name,
        typeof(OwnedEntityBase<object>).Name,
        typeof(PrimitiveCollection<object>).Name,
    };

    // ReSharper disable once CollectionNeverUpdated.Local - can be used in future
    private static readonly HashSet<Type> NonTrackedEntities = new();

    private readonly IChangePublisher _changePublisher;

    private readonly IFunctionContextAccessor _functionContextAccessor;

    private readonly Lazy<FeatureToggles> _lazyToggles;

    private readonly ILog _log;

    private readonly IUserContextAccessor _userContextAccessor;

    public ChangeObserver(IChangePublisher changePublisher,
                          IUserContextAccessor userContextAccessor,
                          IFunctionContextAccessor functionContextAccessor,
                          IAppSettings appSettings,
                          ILog log)
    {
        _changePublisher = changePublisher;
        _userContextAccessor = userContextAccessor;
        _functionContextAccessor = functionContextAccessor;
        _lazyToggles = FeatureToggles.Init(appSettings);
        _log = log;
    }

    public async Task<List<ChangeModel>> GetChanges(DbContext dbContext)
    {
        // is this feature disabled?
        if (_lazyToggles.Value.DisableChangeTracking)
        {
            return null;
        }

        // get changes from the context
        var changes = await GetChanges(dbContext, _log);

        // append inferential metadata to all changes
        AppendInferentialMetadata(changes);

        // available change set
        return changes;
    }

    public async Task Publish(List<ChangeModel> changes)
    {
        // is this feature disabled?
        if (_lazyToggles.Value.DisableChangeTracking)
        {
            return;
        }

        // no changes to publish
        if (changes == null)
        {
            return;
        }

        // publish all the changes
        foreach (var change in changes)
        {
            await _changePublisher.Publish(change);
        }
    }

    private static async Task<List<ChangeModel>> GetChanges(DbContext dbContext, ILog log)
    {
        // fetch the list of observed entities
        var observedEntities = dbContext.ChangeTracker.Entries().Where(IsObserved).ToList();

        // fetch original entities
        var changes = new List<ChangeModel>();
        foreach (var observedEntity in observedEntities)
        {
            // define the object pair
            object objectBefore;
            object objectAfter;

            switch (observedEntity.State)
            {
                case EntityState.Added:
                    objectBefore = null;
                    objectAfter = observedEntity.Entity;
                    break;

                case EntityState.Unchanged: // some unchanged entities may have changed child entities 
                case EntityState.Modified:
                    objectBefore = await dbContext.FetchOriginalDynamic(observedEntity.Entity);
                    objectAfter = observedEntity.Entity;
                    break;

                case EntityState.Deleted:
                    objectBefore = await dbContext.FetchOriginalDynamic(observedEntity.Entity);
                    objectAfter = null;
                    break;

                default:
                    log.Error(messageTemplate: $"Only modification states are tracked. Selected state: {observedEntity.State}");
                    continue;
            }

            // no any objects = no change
            if (objectBefore == null && objectAfter == null)
            {
                log.Error(messageTemplate: $"There is no either the Original object or the Target object to create a change model. Name: {observedEntity.Metadata.Name}");
                continue;
            }

            // create a model with the change and add it to the list of changes
            var change = CreateChangeModel(objectBefore, objectAfter);
            changes.Add(change);
        }

        return changes;

        bool IsObserved(EntityEntry entry)
        {
            // entities that are not part of the commit are not observed
            if (entry.State is EntityState.Detached)
            {
                return false;
            }

            // entities that are unchanged might have been changed via dependents
            if (entry.State is EntityState.Unchanged && IsReallyUnchanged(entry))
            {
                return false;
            }

            // only top-level entities are supported
            if (entry.Metadata.ClrType.IsAssignableTo(typeof(Entity)))
            {
                return !NonTrackedEntities.Contains(entry.Metadata.ClrType);
            }

            // owned entities should be skipped
            if (IsNonTrackedType(entry.Metadata.ClrType))
            {
                return false;
            }

            // fail for undefined use cases
            var message = $"Type is not supported or doesn't have proper markup: {entry.Metadata.ClrType.FullName}{Environment.NewLine}{entry.Entity.ToJson()}";
            log.Error(messageTemplate: message);
            return false;

            static bool IsNonTrackedType(Type type)
            {
                // iterate over the hierarchy
                while (type != null)
                {
                    // is one of the non-tracked types?
                    if (NonTrackedTypes.Contains(type.Name))
                    {
                        return true;
                    }

                    // go to the base type
                    type = type.BaseType;
                }

                // neither the current type nor any of the base types match
                return false;
            }

            static bool IsReallyUnchanged(EntityEntry originalEntry)
            {
                // detect changes within child collections
                foreach (var collection in originalEntry.Collections)
                {
                    // check every entity of a collection
                    foreach (var entity in collection.CurrentValue ?? Enumerable.Empty<object>())
                    {
                        // get entity state from the change tracker
                        var relatedEntry = collection.FindEntry(entity);
                        if (relatedEntry == null)
                        {
                            // this is a safety check: the entry should always be available since it's taken from the change tracker
                            continue;
                        }

                        // the entity is either modified or one of its children is
                        if (relatedEntry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted || !IsReallyUnchanged(relatedEntry))
                        {
                            // changed
                            return false;
                        }
                    }
                }

                // unchanged
                return true;
            }
        }
    }

    private static ChangeModel CreateChangeModel(object before, object after)
    {
        // the JSON serializer to convert the entity to the JSON file
        var serializer = JsonSerializer.Create(SerializerSettings);

        // init
        var changeId = Guid.NewGuid();
        var objectBefore = before == null ? null : JObject.FromObject(before, serializer);
        var objectAfter = after == null ? null : JObject.FromObject(after, serializer);

        // fetch keys
        var jObject = (objectAfter ?? objectBefore)!;
        var entityType = (string)jObject[FieldNameEntityType];
        var entityId = (string)jObject[FieldNameId];
        var entityDocumentType = (string)jObject[FieldNameDocumentType];

        // the resulting change model
        return new()
        {
            // BASICS
            ChangeId = changeId,
            ObjectBefore = objectBefore,
            ObjectAfter = objectAfter,
            ReferenceEntityType = entityType,
            ReferenceEntityId = entityId,
            ReferenceEntityDocumentType = entityDocumentType,

            // TO BE FILLED OUT AT THE METADATA STAGE
            ChangedAt = default,
            ChangedById = default,
            ChangedBy = default,
            FunctionName = default,
            OperationId = default,
            TransactionId = default,
            CorrelationId = default,
        };
    }

    private void AppendInferentialMetadata(List<ChangeModel> changes)
    {
        // the operation ID for all changes
        var operationId = Guid.NewGuid();

        // one timestamp for all changes
        var timestamp = DateTimeOffset.UtcNow;

        // capture the actor
        var userContext = _userContextAccessor.UserContext ?? new()
        {
            EmailAddress = null,
            DisplayName = "Integrations",
        };

        // executing function
        var functionName = default(string);
        var transactionId = default(string);
        var correlationId = default(string);
        if (_functionContextAccessor.FunctionContext is { } functionContext)
        {
            functionName = functionContext.FunctionDefinition.Name;
            transactionId = functionContext.InvocationId;
            correlationId = functionContext.TraceContext.TraceParent;
        }

        // append to all changes
        foreach (var change in changes)
        {
            // append 'when' the change was made
            change.ChangedAt = timestamp;

            // append 'who' made the change
            change.ChangedById = userContext.EmailAddress;
            change.ChangedBy = userContext.DisplayName;

            // append 'what' made the change
            change.FunctionName = functionName;

            // tag this call
            change.OperationId = operationId;

            // append transient info
            change.TransactionId = transactionId;
            change.CorrelationId = correlationId;
        }
    }
}
