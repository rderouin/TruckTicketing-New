using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.Shared.Common.Extensions;

using Trident.Extensions;

namespace SE.BillingService.Domain.InvoiceDelivery.Mapper;

public class InvoiceDeliveryMapper : IInvoiceDeliveryMapper
{
    private static readonly object GlobalLock = new();

    private readonly IExpressionManager _expressionManager;

    private readonly IJsonFiddler _jsonFiddler;

    public InvoiceDeliveryMapper(IJsonFiddler jsonFiddler, IExpressionManager expressionManager)
    {
        _jsonFiddler = jsonFiddler;
        _expressionManager = expressionManager;
    }

    public async Task Map(InvoiceDeliveryContext context)
    {
        // init the target
        context.Medium = new();

        // precompile expressions
        var assembly = PrecompileExpressions(context);
        var requestJson = JObject.FromObject(context.Request);

        // flat tables require going back and filling the blanks
        var backFill = context.DeliveryConfig.MessageAdapterType == MessageAdapterType.Csv;
        var iterations = backFill ? 2 : 1;

        // preprocessor
        var requestCache = new Dictionary<string, object>();
        if (context.DeliveryConfig.IsPreprocessorEnabled == true)
        {
            var newPayload = ApplyExpression(assembly, context.DeliveryConfig.Id, requestJson, context.Request.Payload, context.Request.Payload, requestCache);
            if (newPayload is not JObject payload)
            {
                throw new InvalidOperationException($"The preprocessor must return an object of type 'JObject'. (InvoiceExchangeId: '{context.Config.Id}')");
            }

            context.Request.Payload = payload;
        }

        // execute
        for (var i = 0; i < iterations; i++)
        {
            // address each mapping
            var userCache = new Dictionary<string, object>(requestCache);
            foreach (var mapping in context.DeliveryConfig.Mappings)
            {
                if (mapping.IsDisabled)
                {
                    continue;
                }

                await MapSingle(assembly,
                                requestJson,
                                context.Request.Payload,
                                context.Medium,
                                context.DeliveryConfig.MessageAdapterType,
                                mapping,
                                context.Lookups.SourceFields,
                                context.Lookups.DestinationFields,
                                context.Lookups.ValueFormats,
                                userCache);
            }
        }
    }

    private string GetModuleName(InvoiceExchangeEntity invoiceExchangeEntity)
    {
        // hash of the entire configuration
        using var sha256 = SHA256.Create();
        var hashValue = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(invoiceExchangeEntity))));

        // KEY = ID + HASH
        return $"{invoiceExchangeEntity.Id:N}_{hashValue}";
    }

    private Assembly PrecompileExpressions(InvoiceDeliveryContext context)
    {
        // check if it is already cached
        var moduleName = GetModuleName(context.Config);
        var assembly = _expressionManager.TryGetExisting(moduleName);
        if (assembly != null)
        {
            return assembly;
        }

        lock (GlobalLock)
        {
            // check again inside of this lock, it might be a racing condition
            assembly = _expressionManager.TryGetExisting(moduleName);
            if (assembly != null)
            {
                return assembly;
            }

            // fetch all mappings
            var mappings = new List<InvoiceExchangeMessageFieldMappingEntity>();
            mappings.AddRange(context.Config.InvoiceDeliveryConfiguration.Mappings);
            mappings.AddRange(context.Config.FieldTicketsDeliveryConfiguration.Mappings);

            // extract expressions
            var expressions = mappings.Where(m => m.DestinationUsesValueExpression)
                                      .ToDictionary(m => m.Id, m => m.DestinationValueExpression);

            // preprocessor - invoice
            if (context.Config.InvoiceDeliveryConfiguration.IsPreprocessorEnabled == true)
            {
                expressions[context.Config.InvoiceDeliveryConfiguration.Id] = context.Config.InvoiceDeliveryConfiguration.PreprocessorExpression;
            }

            // preprocessor - field tickets
            if (context.Config.FieldTicketsDeliveryConfiguration.IsPreprocessorEnabled == true)
            {
                expressions[context.Config.FieldTicketsDeliveryConfiguration.Id] = context.Config.FieldTicketsDeliveryConfiguration.PreprocessorExpression;
            }

            // compile them all
            assembly = _expressionManager.CompileExpressions(moduleName, expressions);
        }

        return assembly;
    }

    private Task MapSingle(Assembly assembly,
                           JObject request,
                           JObject source,
                           JObject target,
                           MessageAdapterType adapterType,
                           InvoiceExchangeMessageFieldMappingEntity mapping,
                           IDictionary<Guid, SourceModelFieldEntity> sourceFields,
                           IDictionary<Guid, DestinationModelFieldEntity> destinationFields,
                           IDictionary<Guid, ValueFormatEntity> formats,
                           Dictionary<string, object> userCache)
    {
        string dstJsonPath = null;
        string srcJsonPath = null;

        if ((adapterType == MessageAdapterType.Pidx ||
             adapterType == MessageAdapterType.MailMessage) &&
            mapping.DestinationModelFieldId is not null)
        {
            // fetch the target field's JSON Path
            if (destinationFields.TryGetValue(mapping.DestinationModelFieldId.Value, out var destinationModelField) && destinationModelField != null)
            {
                dstJsonPath = destinationModelField.JsonPath;
            }
            else
            {
                throw new InvalidOperationException($"Destination mapping has not been found. (DestinationModelFieldId: '{mapping.DestinationModelFieldId}')");
            }
        }

        if (adapterType == MessageAdapterType.Csv && mapping.DestinationHeaderTitle is not null)
        {
            dstJsonPath = $"$.Rows[a].['{mapping.DestinationHeaderTitle}']";
            mapping.DestinationPlacementHint = "a=*";
        }

        // target field contains IL instruction on how to write it
        if (dstJsonPath is null)
        {
            throw new InvalidOperationException("Target field must be provided. (Target JSON Path is missing)");
        }

        // source data
        SourceModelFieldEntity sourceField = null;
        List<JValue> originalSourceValues = null;
        if (mapping.SourceModelFieldId.HasValue && sourceFields.TryGetValue(mapping.SourceModelFieldId.Value, out sourceField))
        {
            srcJsonPath = sourceField.JsonPath;
            originalSourceValues = _jsonFiddler.ReadValue(sourceField.IsGlobal ? request : source, Regex.Replace(srcJsonPath ?? string.Empty, @"\*+", "*")).ToList();
        }

        // fetch the value
        var values = mapping switch
                     {
                         // use a constant if provided
                         { DestinationUsesValueExpression: false } when mapping.DestinationFormatId == Guid.Empty =>
                             originalSourceValues != null
                                 ? JObject.FromObject(new { Constants = Enumerable.Repeat(mapping.DestinationConstantValue, originalSourceValues.Count).ToArray() }).SelectTokens("$.Constants[*]")
                                          .Cast<JValue>()
                                 : new JValue[] { new(mapping.DestinationConstantValue) },

                         // default behavior
                         { SourceModelFieldId: { } } when sourceField != null =>
                             originalSourceValues,

                         // expression only, without a source value
                         { DestinationUsesValueExpression: true, DestinationValueExpression: { }, SourceModelFieldId: null } =>
                             new[] { JValue.CreateNull() },

                         // no source, no constant => what to do?
                         _ => throw new NotSupportedException($"Mapping configuration is not supported. (Field Mapping ID: {mapping.Id})"),
                     };

        // fetch format options
        ValueFormatEntity format = null;
        var hasFormatter = mapping.DestinationFormatId.HasValue && formats.TryGetValue(mapping.DestinationFormatId.Value, out format);

        // placement hint selection
        Dictionary<string, int?> placementHint;
        Dictionary<string, string> dynamicIndexNames;

        // mapping hint:  a0=1,a1=*,a2=0
        // translated to:
        //   a0 - array 'a0' with index '1'
        //   a1 - dynamic array matched the source dynamic array
        //   a2 - array 'a2' with index '0'
        if (mapping.DestinationPlacementHint.HasText())
        {
            (placementHint, dynamicIndexNames) = ParseDestinationPlacementHint(mapping.DestinationPlacementHint);
        }
        else
        {
            placementHint = new();
            dynamicIndexNames = new();
        }

        // path metadata
        var dstPathMetadata = JsonFiddler.ParseJsonPath(dstJsonPath);
        var srcPathMetadata = JsonFiddler.ParseJsonPath(srcJsonPath);
        var indexLookup = CreateIndexLookup(dstPathMetadata, srcPathMetadata, dynamicIndexNames);

        // NOTE: the multiplicity mapping from source to destination property
        // source:0, destination:single     => no action
        // source:0, destination:multiple   => no action
        // source:1, destination:single     => writes 1 item
        // source:1, destination:multiple   => writes 1 item
        // source:M, destination:single     => last one wins
        // source:M, destination:multiple   => writes all items into target
        //
        // examples:
        //      - source query for a single item:       $.invoice_number
        //      - source query for many items:          $.lines[*].info
        //      - destination path for a single item:   $.invoice.number
        //      - destination path for multiple items:  $.invoice.lines[*].info

        // write all found values
        var sourceValues = values.Select((value, index) => (value, index)).ToList();
        var hasManySourceValues = sourceValues.Count > 1;
        var hasDynamicIndexNames = dynamicIndexNames.Any();
        var dynamicIndexNamesSet = new HashSet<string>(dynamicIndexNames.Values);
        foreach (var item in sourceValues)
        {
            // fetch the value
            var value = mapping switch
                        {
                            // no processing needed for a constant
                            { DestinationUsesValueExpression: false } when mapping.DestinationFormatId == Guid.Empty =>
                                mapping.DestinationConstantValue,

                            // formatter is given, use it
                            { DestinationUsesValueExpression: false, DestinationFormatId: { } } when hasFormatter && format != null && item.value.Value != null =>
                                ApplyFormatting(format, item.value),

                            // execute C# expression
                            { DestinationUsesValueExpression: true, DestinationValueExpression: { } } =>
                                ApplyExpression(assembly, mapping.Id, request, item.value, item.value.Value, userCache),

                            // fallback => no action, raw value
                            _ => item.value.Value,
                        };

            // get path metadata for the fetched value
            var valuePathMetadata = mapping switch
                                    {
                                        // for constants, rely on the source values metadata if provided
                                        { DestinationUsesValueExpression: false } when mapping.DestinationFormatId == Guid.Empty && originalSourceValues?.Count > 0 =>
                                            GetMetadata(originalSourceValues[item.index]),

                                        // default logic => take path metadata from the JSON value itself
                                        _ => GetMetadata(item.value),
                                    };

            // write one or multiple values
            const int maxIterations = 1_000_000;
            var currentIteration = 0;
            var undefinedIndex = 0;
            bool hasManyTargets;
            do
            {
                // prevent infinite loops
                if (currentIteration++ > maxIterations)
                {
                    var context = new
                    {
                        request,
                        target,
                        adapterType,
                        mapping,
                        srcJsonPath,
                        dstJsonPath,
                        placementHint,
                        dynamicIndexNames,
                        dstPathMetadata,
                        srcPathMetadata,
                        indexLookup,
                        undefinedIndex,
                        userCache,
                    };

                    var info = JsonConvert.SerializeObject(context, Formatting.Indented);
                    throw new InvalidOperationException($"Mapper exceeded iterations, context info:{Environment.NewLine}{Environment.NewLine}{info}");
                }

                // update placement hints
                if (valuePathMetadata.Count < 1 ||
                    (mapping.DestinationFormatId == Guid.Empty && !mapping.DestinationUsesValueExpression && !hasManySourceValues))
                {
                    foreach (var dynamicIndexName in dynamicIndexNames)
                    {
                        placementHint[dynamicIndexName.Value] = undefinedIndex++;
                    }
                }
                else
                {
                    // map array indexes, driven by the path of the value 
                    foreach (var arrayItemMetadata in valuePathMetadata)
                    {
                        // fetch the index metadata for the array item of the value
                        if (indexLookup.TryGetValue(arrayItemMetadata.element, out var indexMeta))
                        {
                            // set the target placement index based on the index lookup table, or copy a single source value to many targets
                            placementHint[indexMeta.rank] = hasManySourceValues ? arrayItemMetadata.index : undefinedIndex++;
                        }
                    }
                }

                // put the value into the new object
                hasManyTargets = _jsonFiddler.WriteValue(target, dstJsonPath, value, placementHint, dynamicIndexNamesSet);

                // break, go to the next source value (M:M mapping or M:1 mapping)
                if (hasManySourceValues)
                {
                    break;
                }

                // continue (1:M mapping or 1:1 mapping)
            } while (hasManyTargets && hasDynamicIndexNames);
        }

        return Task.CompletedTask;
    }

    private Dictionary<string, (string property, string rank)> CreateIndexLookup(List<(string element, string index)> dstPathMetadata,
                                                                                 List<(string element, string index)> srcPathMetadata,
                                                                                 Dictionary<string, string> dynamicIndexNames)
    {
        Dictionary<string, (string property, string rank)> lookup = new();

        // iterate over the source multi-array mapping, e.g. $.Lines[*].Tax[**] => lines=a1,tax=a2
        foreach (var srcItem in srcPathMetadata.Where(i => i.index.HasText()))
        {
            // source index might be mapped and customized, pick the new name
            var srcIndex = srcItem.index;
            if (dynamicIndexNames.TryGetValue(srcIndex, out var dynamicIndexName))
            {
                srcIndex = dynamicIndexName;
            }

            // find the corresponding index in the target metadata and add the index mapping into the lookup table
            var dst = dstPathMetadata.FirstOrDefault(d => d.index.HasText() && d.index == srcIndex);
            if (dst != default)
            {
                // mapping:
                //   [lines]=(invoice_details,a1)
                //   [tax]=(tax,a2)
                lookup[srcItem.element] = (dst.element, dst.index);
            }
        }

        // destination might contain static indexes
        foreach (var dstItem in dstPathMetadata.Where(i => i.index.HasText()))
        {
            // skip already mapped indexes
            if (lookup.Values.Any(e => e.property == dstItem.element))
            {
                continue;
            }

            // add static indexes
            lookup[dstItem.element] = (dstItem.element, dstItem.index);
        }

        return lookup;
    }

    private (Dictionary<string, int?> placementHint, Dictionary<string, string> dynamicIndexNames) ParseDestinationPlacementHint(string value)
    {
        var placementHint = new Dictionary<string, int?>();
        var dynamicIndexNames = new Dictionary<string, string>();

        // split into kvp groups
        var groups = value.Split(",", StringSplitOptions.RemoveEmptyEntries)
                          .Select(v => v.Trim())
                          .ToList();

        // process each
        foreach (var group in groups)
        {
            // format: 
            var kvp = group.Split("=", StringSplitOptions.RemoveEmptyEntries)
                           .Select(v => v.Trim())
                           .ToList();

            // it has to be a specified value, otherwise it's dynamic and doesn't need config
            if (kvp.Count != 2)
            {
                continue;
            }

            // configure it
            if (int.TryParse(kvp[1], out var index))
            {
                // if it's a static integer value (array index), then configure it
                placementHint[kvp[0]] = index;
            }
            else if (Regex.Match(kvp[1], @"\*+") is { Success: true } m)
            {
                // if it's a dynamic index for a specific array, notify parent about it
                dynamicIndexNames.Add(m.Value, kvp[0]);
            }
        }

        return (placementHint, dynamicIndexNames);
    }

    private string ApplyFormatting(ValueFormatEntity format, JValue value)
    {
        // fetch the assumed source type
        var typeName = format.SourceType.HasText() ? format.SourceType : "System.String";

        // use JSON to get to the right type
        var json = JsonConvert.SerializeObject(value.Value);
        var typedValue = JsonConvert.DeserializeObject(json, Type.GetType(typeName) ?? typeof(string));

        // format using the typed object
        return string.Format(CultureInfo.InvariantCulture, format.ValueExpression, typedValue);
    }

    private object ApplyExpression(Assembly assembly, Guid expressionId, JObject request, JToken item, object value, Dictionary<string, object> cache)
    {
        // fetch the expression
        var func = _expressionManager.GetExpression(assembly, expressionId);
        if (func == null)
        {
            return value;
        }

        // prep arguments
        var refs = new Dictionary<string, object>
        {
            ["request"] = request,
            ["item"] = item,
            ["value"] = value,
        };

        // execute
        try
        {
            return func(refs, cache);
        }
        catch (Exception x)
        {
            var info = new
            {
                expressionId,
                request,
                item,
                value,
                cache,
            }.ToJson();

            throw new InvalidOperationException($"Unable to execute the expression. Details:{Environment.NewLine}{Environment.NewLine}{info}", x);
        }
    }

    private List<(string element, int index)> GetMetadata(JToken token)
    {
        // blanks = no mapping
        if (token == null || !token.Path.HasText())
        {
            return new();
        }

        // keep only arrays
        return GetMetadata($"$.{token.Path}");
    }

    private List<(string element, int index)> GetMetadata(string jsonPath)
    {
        // blanks = no mapping
        if (jsonPath.HasText() == false)
        {
            return new();
        }

        // dissect the JSON path
        var pathMetadata = JsonFiddler.ParseJsonPath(jsonPath);

        // keep only arrays
        return pathMetadata.Where(p => p.index != null).Select(p => (p.element, int.Parse(p.index))).ToList();
    }
}
