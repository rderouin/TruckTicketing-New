using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using SE.Shared.Common.Changes;

using Trident.Domain;

namespace SE.Shared.Domain.Entities.Changes;

public class ChangeService : IChangeService
{
    private static readonly HashSet<string> TridentPrimitiveCollectionSet = new()
    {
        nameof(PrimitiveCollection<object>.Key),
        nameof(PrimitiveCollection<object>.Raw),
        nameof(PrimitiveCollection<object>.List),
    };

    private readonly IChangeComparer _changeComparer;

    public ChangeService(IChangeComparer changeComparer)
    {
        _changeComparer = changeComparer;
    }

    public List<FieldChange> CompareObjects(Type type, JObject source, JObject target, ChangeConfiguration config)
    {
        // pre-process the JSON document
        GoThrough(source, PreProcessNode);
        GoThrough(target, PreProcessNode);

        // get the difference from the objects
        var changes = _changeComparer.Compare(source, target);

        // filter the member if not tracking all fields
        if (!config.TrackAllFields)
        {
            changes = changes.Where(change => config.MembersToInclude.Contains(change.GetAgnosticPath())).ToList();
        }

        return changes;
    }

    private void PreProcessNode(JToken token)
    {
        // replace the Trident's primitive collection with .NET primitive collection
        ReplaceTridentPrimitiveCollection(token);
    }

    internal static void GoThrough(JToken token, Action<JToken> action)
    {
        if (token is null)
        {
            return;
        }

        if (token is JObject jObject)
        {
            foreach (var property in jObject.Properties().ToList())
            {
                GoThrough(property.Value, action);
                action(property.Value);
            }
        }

        if (token is JArray jArray)
        {
            foreach (var item in jArray)
            {
                GoThrough(item, action);
                action(item);
            }
        }
    }

    internal static bool IsTridentPrimitiveCollection(JToken token)
    {
        if (token is JObject jObject)
        {
            var properties = jObject.Properties().ToList();
            if (properties.Count == 3 && properties.All(p => TridentPrimitiveCollectionSet.Contains(p.Name)))
            {
                return true;
            }
        }

        return false;
    }

    internal static void ReplaceTridentPrimitiveCollection(JToken token)
    {
        if (IsTridentPrimitiveCollection(token) && token.Parent is JProperty jProperty)
        {
            var internalList = token[nameof(PrimitiveCollection<object>.List)];
            jProperty.Value = internalList;
        }
    }
}
