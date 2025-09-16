using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using SE.Shared.Common.Lookups;

using Trident.Contracts.Configuration;
using Trident.Extensions;
using Trident.Logging;

namespace SE.Shared.Domain.Entities.Changes;

public class ChangeComparer : IChangeComparer
{
    private readonly string _collectionIdentifier;

    private readonly ILog _log;

    public ChangeComparer(IAppSettings appSettings, ILog log)
    {
        _log = log;
        _collectionIdentifier = appSettings.GetKeyOrDefault("ChangeTracking:CollectionIdentifier", "Id");
    }

    public List<FieldChange> Compare(JToken source, JToken target)
    {
        try
        {
            var changedProperties = new List<FieldChange>();
            CompareTokens(source, target, changedProperties);
            return changedProperties;
        }
        catch (Exception e)
        {
            var inputs = new
            {
                source,
                target,
            }.ToJson();

            _log.Error(exception: e, messageTemplate: $"Unable to compare objects!{Environment.NewLine}{inputs}");

            throw;
        }
    }

    private void CompareTokens(JToken source, JToken target, List<FieldChange> changes)
    {
        // type-fix
        source = source?.Type == JTokenType.Null ? null : source;
        target = target?.Type == JTokenType.Null ? null : target;

        // type rubbing
        source = EnsureTypeSafety(source);
        target = EnsureTypeSafety(target);

        // the types match, check if the entire hierarchy is the same
        if (JToken.DeepEquals(source, target))
        {
            // full match = no action
            return;
        }

        // select further action based on the object types
        switch (source, target)
        {
            // compare objects
            case (null or JObject, null or JObject):
                CompareObjects((JObject)source, (JObject)target, changes);
                return;

            // compare arrays
            case (null or JArray, null or JArray):
                CompareArrays((JArray)source, (JArray)target, changes);
                return;

            // compare values
            case (null or JValue, null or JValue):
                LogValues((JValue)source, (JValue)target, changes);
                return;

            // something else = not supported!
            default:
                throw new InvalidOperationException($"{nameof(ChangeComparer)}.{nameof(CompareTokens)}: This condition should never be met!");
        }

        static JToken EnsureTypeSafety(JToken token)
        {
            // JConstructor - JSON constructor, to support JSON deviations
            // JRaw - a raw JSON string, unparsed
            // JProperty - a JSON property, not supported in the standalone context
            // JContainer - an abstract container class for other JToken-based classes, DO NOT filter out this class
            return token is JConstructor or JRaw or JProperty ? null : token;
        }
    }

    private void CompareObjects(JObject source, JObject target, List<FieldChange> changes)
    {
        // list of all properties
        var names = new HashSet<string>();
        source?.Properties().ToList().ForEach(p => names.Add(p.Name));
        target?.Properties().ToList().ForEach(p => names.Add(p.Name));

        // compare all properties
        foreach (var name in names)
        {
            CompareTokens(source?.Property(name)?.Value, target?.Property(name)?.Value, changes);
        }
    }

    private void CompareArrays(JArray source, JArray target, List<FieldChange> changes)
    {
        // check if the collection is made of primitives
        var jArray = target ?? source;
        if (jArray.Where(token => token.Type != JTokenType.Null)
                  .All(token => token.Type == JTokenType.Object))
        {
            CompareObjectArrays(source, target, changes);
        }
        else
        {
            ComparePrimitiveArrays(source, target, changes);
        }
    }

    private void LogValues(JValue source, JValue target, List<FieldChange> changes)
    {
        // both objects are blank = skip operation
        if (source == null && target == null)
        {
            return;
        }

        // metadata
        var (fieldLocation, fieldName, objectId) = GetFieldInfo(target ?? source);
        var valueBefore = FormatValue(source);
        var valueAfter = FormatValue(target);
        var operation = GetOperation(source, target);

        // log the change change
        changes.Add(new(source, target)
        {
            FieldLocation = fieldLocation,
            FieldName = fieldName,
            ValueBefore = valueBefore,
            ValueAfter = valueAfter,
            Operation = operation,
            ObjectId = objectId,
        });
    }

    private (string fieldLocation, string fieldName, string objectId) GetFieldInfo(JToken token)
    {
        if (token == null)
        {
            return (string.Empty, string.Empty, null);
        }

        if (token.Parent is JProperty property)
        {
            return (property.Parent?.Path ?? string.Empty, property.Name, FetchAdjacentId(token, _collectionIdentifier));
        }

        if (token.Parent is JArray array)
        {
            return (token.Path, string.Empty, FetchAdjacentId(token, _collectionIdentifier));
        }

        return GetFieldInfo(token.Parent);

        static string FetchAdjacentId(JToken refToken, string idPropertyName)
        {
            // starting position = self
            var pointer = refToken;

            // iterate over the hierarchy
            while (pointer != null)
            {
                // within the hierarchy, there should be a JObject that holds the 'ID' property
                if (pointer is JObject jObject && jObject.TryGetValue(idPropertyName, out var refId))
                {
                    // convert the token into a string
                    return refId.ToObject<string>();
                }

                // traverse to the root
                pointer = pointer.Parent;
            }

            return null;
        }
    }

    private string FormatValue(JValue jValue)
    {
        // null handling
        if (jValue == null || jValue.Type == JTokenType.Null)
        {
            return null;
        }

        // special type handling
        switch (jValue.Type)
        {
            case JTokenType.Date:
                return jValue.ToObject<DateTimeOffset>().ToString("O");
        }

        // all types should be convertible to a string
        return jValue.ToObject<string>();
    }

    private ChangeOperation GetOperation(JToken source, JToken target)
    {
        switch (source, target)
        {
            case (null, not null):
                return ChangeOperation.Added;

            case (not null, null):
                return ChangeOperation.Deleted;

            case (not null, not null):
                return ChangeOperation.Updated;

            default:
                return ChangeOperation.Unknown;
        }
    }

    private void ComparePrimitiveArrays(JArray sourceArray, JArray targetArray, List<FieldChange> changes)
    {
        var sourceValueArray = GetCleanListOfValues(sourceArray);
        var targetValueArray = GetCleanListOfValues(targetArray);

        // log items that exist only in the target array - added items
        foreach (var targetValue in targetValueArray)
        {
            if (sourceValueArray.Any(sourceValue => CompareValues(sourceValue, targetValue)))
            {
                continue;
            }

            LogValues(null, targetValue, changes);
        }

        // log items that exist only in the source array - deleted items
        foreach (var sourceValue in sourceValueArray)
        {
            if (targetValueArray.Any(targetValue => CompareValues(sourceValue, targetValue)))
            {
                continue;
            }

            LogValues(sourceValue, null, changes);
        }

        static List<JValue> GetCleanListOfValues(JArray array)
        {
            return array?.Where(item => item is JValue { Value: not null }).Cast<JValue>().ToList() ?? new();
        }

        static bool CompareValues(JValue source, JValue target)
        {
            return EqualityComparer<object>.Default.Equals(source?.Value, target?.Value);
        }
    }

    private void CompareObjectArrays(JArray sourceArray, JArray targetArray, List<FieldChange> changes)
    {
        // create object maps
        var sourceLookup = GetCleanMapOfObjects(sourceArray, _collectionIdentifier);
        var targetLookup = GetCleanMapOfObjects(targetArray, _collectionIdentifier);

        // all possible keys
        var keySet = sourceLookup.Select(e => e.Key).Concat(targetLookup.Select(e => e.Key)).ToHashSet();

        // compare each object by key with other object by key
        foreach (var key in keySet)
        {
            foreach (var sourceObject in WithBlankOnEmptyArray(sourceLookup[key]))
            foreach (var targetObject in WithBlankOnEmptyArray(targetLookup[key]))
            {
                CompareObjects(sourceObject, targetObject, changes);
            }
        }

        static ILookup<string, JObject> GetCleanMapOfObjects(JArray array, string property)
        {
            return (array ?? new()).OfType<JObject>().ToLookup(t => t[property]?.ToObject<string>());
        }

        static IEnumerable<JObject> WithBlankOnEmptyArray(IEnumerable<JObject> enumerable)
        {
            var evaluated = false;

            // go over the enumerable, the pass-through block for non-empty collections
            foreach (var item in enumerable)
            {
                evaluated = true;
                yield return item;
            }

            // generate a null-object if the collection is blank
            if (!evaluated)
            {
                yield return null;
            }
        }
    }
}
