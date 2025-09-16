using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using SE.Shared.Common.Extensions;

using Trident.Extensions;

namespace SE.BillingService.Domain.InvoiceDelivery.Mapper;

public class JsonFiddler : IJsonFiddler
{
    // language=regexp
    /// <summary>
    ///     Pattern for a complete JSON Path expression.
    /// </summary>
    private const string TargetJsonPathPattern = @"(?x)
^
(?<root>\$)
(?<element>\.
    (?<property>
        (?<p>
            (?<propertyName>[a-zA-Z0-9_]+)
            |
            (?<propertySpec>
                (?<pb>\[\')
                (?<propertyName>.*?)
                (?<-pb>\'\])
            )
        )
        (?<indexer>
	        (?<b>\[)
	        (?<index>
		        (?<indexDefault>\*+)
				|
				(?<indexStatic>\d+)
				|
				(?<indexFilter>[a-zA-Z0-9_]+(?<l>-\d+)?)
	        )
	        (?<-b>\])
        )?
    )
)+
$
";

    // language=regexp
    /// <summary>
    ///     Pattern for a single element of a JSON Path expression.
    /// </summary>
    private const string PropertyPattern = @"(?x)
(?<p>
    (?<propertyName>[a-zA-Z0-9_]+)
    |
    (?<propertySpec>
        (?<pb>\[\')
        (?<propertyName>.*?)
        (?<-pb>\'\])
    )
)
(?<indexer>
	(?<b>\[)
	(?<index>
		(?<indexDefault>\*+)
		|
		(?<indexStatic>\d+)
		|
		(?<indexFilter>[a-zA-Z0-9_]+(?<l>-\d+)?)
	)
	(?<-b>\])
)?
";

    private static readonly Regex JsonPathRegex = new(TargetJsonPathPattern, RegexOptions.Compiled, TimeSpan.FromMinutes(1));

    private static readonly Regex PropertyRegex = new(PropertyPattern, RegexOptions.Compiled, TimeSpan.FromMinutes(1));

    public IEnumerable<JValue> ReadValue(JObject source, string path)
    {
        // uses a JSON Path expression
        return source.SelectTokens(path).Cast<JValue>();
    }

    public bool WriteValue(JObject target, string path, object value, IDictionary<string, int?> placementHint, ISet<string> dynamicIndexNamesSet)
    {
        var hasMore = false;

        // validate the path
        var match = JsonPathRegex.Match(path);
        if (!match.Success)
        {
            throw new InvalidOperationException("JSON path is invalid.");
        }

        // extract JSON path elements
        var propertyMatches = PropertyRegex.Matches(path).ToList();
        EnsureIntegrity(propertyMatches);

        // iterate over individual elements
        JToken lastToken = target;
        foreach (var propertyMatch in propertyMatches)
        {
            // match values
            var property = propertyMatch.Groups["propertyName"].Value;
            var index = propertyMatch.Groups["indexFilter"].Success
                            ? propertyMatch.Groups["indexFilter"].Value
                            : propertyMatch.Groups["index"].Value;

            // update the object
            var (token, parent) = EnsurePathExists(lastToken, property, index, placementHint);

            // 1=>M copy
            if (parent is JArray array)
            {
                if (placementHint?.TryGetValue(index, out var indexValue) == true)
                {
                    if (indexValue < array.Count - 1)
                    {
                        hasMore = dynamicIndexNamesSet?.Contains(index) == true;
                    }
                }
            }

            // move on
            lastToken = token;
        }

        // set the value
        lastToken.Replace(WrapValue(value));

        return hasMore;
    }

    public static List<(string element, string index)> ParseJsonPath(string path)
    {
        List<(string element, string index)> list = new();

        // no path = no metadata
        if (!path.HasText())
        {
            return list;
        }

        // validate the entire expression is valid
        var pathMatch = JsonPathRegex.Match(path);
        if (!pathMatch.Success)
        {
            return list;
        }

        // process each element separately
        foreach (Capture c in pathMatch.Groups["property"].Captures)
        {
            // parse a single property
            var propertyMatch = PropertyRegex.Match(c.Value);
            if (!propertyMatch.Success)
            {
                continue;
            }

            // fetch values
            var propertyNameGroup = propertyMatch.Groups["propertyName"];
            var indexGroup = propertyMatch.Groups["index"];

            // add the property to the list
            list.Add((propertyNameGroup.Value, indexGroup.Success ? indexGroup.Value : null));
        }

        return list;
    }

    private void EnsureIntegrity(List<Match> matches)
    {
        // ensure all match
        if (matches.Any(m => !m.Success))
        {
            throw new InvalidOperationException("Invalid JSON Path expression.");
        }

        // default index must be defined only once
        var defaultIndices = matches.Where(m => m.Groups["indexDefault"].Success)
                                    .Select(m => m.Groups["indexDefault"].Value)
                                    .ToList();

        if (defaultIndices.Count > 1)
        {
            throw new InvalidOperationException("A default index should be defined only once.");
        }

        // named indices should be unique
        var namedIndices = matches.Where(m => m.Groups["indexFilter"].Success)
                                  .Select(m => m.Groups["indexFilter"].Value)
                                  .ToList();

        if (namedIndices.Count != namedIndices.Distinct().Count())
        {
            throw new InvalidOperationException("Named indices should be unique");
        }

        // dynamic and named cannot be combined
        if (defaultIndices.Count > 0 && namedIndices.Count > 0)
        {
            throw new InvalidOperationException("Default and named indices cannot be combined in one expression.");
        }
    }

    private (JToken token, JToken parent) EnsurePathExists(JToken token, string property, string index, IDictionary<string, int?> placementHint)
    {
        // replace the terminator with an object
        if (token is JValue val)
        {
            token = new JObject();
            val.Replace(token);
        }

        // ensure working with an object
        if (token is not JObject j)
        {
            throw new InvalidOperationException();
        }

        // check if already exists
        if (j.TryGetValue(property, out var existingToken))
        {
            // if a property is an array, ensure it has a correct item
            return existingToken is JArray array ? EnsureArrayItemExists(array, index, placementHint) : (existingToken, j);
        }

        // add an array property
        if (!string.IsNullOrWhiteSpace(index))
        {
            var array = new JArray();
            j.Add(property, array);
            return EnsureArrayItemExists(array, index, placementHint);
        }

        // add an object property
        var value = new JValue(default(object));
        j.Add(property, value);
        return (value, j);
    }

    private (JToken token, JToken parent) EnsureArrayItemExists(JArray array, string index, IDictionary<string, int?> placementHint)
    {
        array.GuardIsNotNull(nameof(array));
        index.GuardIsNotNull(nameof(index));

        // NOTE:
        // for static values: ensure that an item exists at the given index value
        // for tagged arrays: add a new item if the index and the tag matches
        // for wildcard tags: always add a new item; n-array map

        // static index
        if (int.TryParse(index, out var i))
        {
            // ensure the size is correct
            return EnsureArrayItemExistsAtIndex(array, i);
        }

        // if the index hint specified, use specific item
        if (placementHint != null && placementHint.TryGetValue(index!, out var indexHint))
        {
            return EnsureArrayItemExistsAtIndex(array, indexHint ?? array.Count);
        }

        // when no index hint is given, append a new item and return it
        if (index == "*")
        {
            return EnsureArrayItemExistsAtIndex(array, array.Count);
        }

        // fallback: have at least one item in the array and return the last item from the array
        if (!array.HasValues)
        {
            var item2 = new JValue(default(object));
            array.Add(item2);
        }

        // no array instruction: return the last item
        return (array.Last(), array);
    }

    private (JToken token, JToken parent) EnsureArrayItemExistsAtIndex(JArray array, int i)
    {
        // create as many array items as needed
        while (i >= array.Count)
        {
            array.Add(new JValue(default(object)));
        }

        return (array[i], array);
    }

    public static JValue WrapValue(object value)
    {
        if (value == null)
        {
            return new((string)null);
        }

        return value switch
               {
                   string s => new(s),
                   char c => new(c),
                   long l => new(l),
                   int i => new(i),
                   short s => new(s),
                   sbyte sb => new(sb),
                   ulong ul => new(ul),
                   uint ui => new(ui),
                   ushort us => new(us),
                   byte b => new(b),
                   decimal d => new(d),
                   double d => new(d),
                   float f => new(f),
                   bool b => new(b),
                   DateTime dt => new(dt),
                   DateTimeOffset dto => new(dto),
                   TimeSpan ts => new(ts),
                   Guid g => new(g),
                   Uri uri => new(uri),
                   _ => throw new NotSupportedException($"Type '{value?.GetType().Name}' is not supported."),
               };
    }
}
