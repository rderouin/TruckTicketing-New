//
//  ______  ______   __  __   ______   __  __   ______  __   ______   __  __   ______  ______  __   __   __   ______   
// /\__  _\/\  == \ /\ \/\ \ /\  ___\ /\ \/ /  /\__  _\/\ \ /\  ___\ /\ \/ /  /\  ___\/\__  _\/\ \ /\ "-.\ \ /\  ___\  
// \/_/\ \/\ \  __< \ \ \_\ \\ \ \____\ \  _"-.\/_/\ \/\ \ \\ \ \____\ \  _"-.\ \  __\\/_/\ \/\ \ \\ \ \-.  \\ \ \__ \ 
//    \ \_\ \ \_\ \_\\ \_____\\ \_____\\ \_\ \_\  \ \_\ \ \_\\ \_____\\ \_\ \_\\ \_____\ \ \_\ \ \_\\ \_\\"\_\\ \_____\
//     \/_/  \/_/ /_/ \/_____/ \/_____/ \/_/\/_/   \/_/  \/_/ \/_____/ \/_/\/_/ \/_____/  \/_/  \/_/ \/_/ \/_/ \/_____/
//                                                                                                                     
//

#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: Microsoft.Azure.Cosmos, 3.37.0"
#r "nuget: Polly, 8.1.0"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Retry;

////////////////////////////// CUT HERE //////////////////////////////

public static class CosmosTools
{
    public static async Task QueryStreaming(Container container, (ResiliencePipeline<FeedResponse<JObject>> feedPolicy, ResiliencePipeline genericPolicy) policySet, string query, string partitionKey, Func<FeedResponse<JObject>, Task> onFetch, Dictionary<string, object> parameters = null, QueryRequestOptions options = null)
    {
        await policySet.genericPolicy.ExecuteAsync(async t1 =>
        {
            // the query text
            var queryDefinition = new QueryDefinition(query);

            // options
            parameters ??= new();
            options ??= new();

            // the partition key for the query
            if (!string.IsNullOrEmpty(partitionKey))
            {
                options.PartitionKey = new PartitionKey(partitionKey);
            }

            // the query parameters
            foreach (var pair in parameters)
            {
                queryDefinition.WithParameter(pair.Key, pair.Value);
            }

            // iterator
            using var feedIterator = container.GetItemQueryIterator<JObject>(queryDefinition, requestOptions: options);
            while (feedIterator.HasMoreResults)
            {
                var feedResponse = await policySet.feedPolicy.ExecuteAsync(async t2 => await feedIterator.ReadNextAsync());
                await onFetch(feedResponse);
            }
        });
    }

    public static async Task<List<JObject>> QueryAll(Container container, (ResiliencePipeline<FeedResponse<JObject>> feedPolicy, ResiliencePipeline genericPolicy) policySet, string query, string partitionKey, Dictionary<string, object> parameters = null, QueryRequestOptions options = null)
    {
        // aggregator
        var documents = new List<JObject>();

        // forward all items into a list
        await QueryStreaming(container, policySet, query, partitionKey, AddItems, parameters, options);

        // all documents
        return documents;

        Task AddItems(FeedResponse<JObject> items)
        {
            documents.AddRange(items);
            return Task.CompletedTask;
        }
    }

    public static async Task<JObject> QueryOne(Container container, (ResiliencePipeline<FeedResponse<JObject>> feedPolicy, ResiliencePipeline genericPolicy) policySet, string query, string partitionKey, Dictionary<string, object> parameters = null, QueryRequestOptions options = null)
    {
        var items = await QueryAll(container, policySet, query, partitionKey, parameters, options);
        return items.FirstOrDefault();
    }

    public static async Task PatchProperty<T>(Container container, ResiliencePipeline genericPolicy, string id, string partitionKey, string propertyPath, T value)
    {
        await genericPolicy.ExecuteAsync(async t =>
        {
            await container.PatchItemAsync<JObject>(id, new PartitionKey(partitionKey), new List<PatchOperation>
            {
                PatchOperation.Add(propertyPath, value),
            });
        });
    }

    public static async Task PatchProperties<T>(Container container, ResiliencePipeline genericPolicy, string id, string partitionKey, Dictionary<string, T> propertyPathToValueMap)
    {
        var patchOps = new List<PatchOperation>();
        foreach (var propertyToValueEntry in propertyPathToValueMap)
        {
            patchOps.Add(PatchOperation.Add(propertyToValueEntry.Key, propertyToValueEntry.Value));
        }
        await genericPolicy.ExecuteAsync(async t =>
                                         {
                                             await container.PatchItemAsync<JObject>(id, new PartitionKey(partitionKey), patchOps);
                                         });
    }

    public static async Task<JObject> UpsertEntity(Container container, JObject doc, string id, string partitionKey)
    {
        if (!doc.ContainsKey("id"))
        {
            doc["id"] = id;
        }

        var pk = new PartitionKey(partitionKey);

        var insertedItem = await container.UpsertItemAsync<JObject>(doc, pk);

        return insertedItem;
    }

    public static async Task DeleteEntity(Container container, string id, string partitionKey = null)
    {
        var pk = string.IsNullOrWhiteSpace(partitionKey) ? PartitionKey.None : new PartitionKey(partitionKey);

        await container.DeleteItemAsync<JObject>(id, pk);

        return;
    }
}

public static class ResiliencePipelines
{
    public static ResiliencePipeline<FeedResponse<JObject>> SetupCosmosFeedResiliencePipeline(Func<OnRetryArguments<FeedResponse<JObject>>, ValueTask> onRetry)
    {
        return new ResiliencePipelineBuilder<FeedResponse<JObject>>().AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder<FeedResponse<JObject>>().HandleResult(r => IsTransientCode(r.StatusCode)).Handle<CosmosException>(e => IsTransientCode(e.StatusCode)),
            MaxRetryAttempts = 10,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Linear,
            MaxDelay = TimeSpan.FromMinutes(1),
            UseJitter = true,
            OnRetry = onRetry,
        }).Build();
    }

    public static ResiliencePipeline SetupCosmosGenericResiliencePipeline(Func<OnRetryArguments<object>, ValueTask> onRetry)
    {
        return new ResiliencePipelineBuilder().AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder().Handle<CosmosException>(e => IsTransientCode(e.StatusCode)),
            MaxRetryAttempts = 10,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Linear,
            MaxDelay = TimeSpan.FromMinutes(1),
            UseJitter = true,
            OnRetry = onRetry,
        }).Build();
    }

    private static bool IsTransientCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.TooManyRequests or
                             HttpStatusCode.RequestTimeout or
                             HttpStatusCode.ServiceUnavailable or
                             HttpStatusCode.Gone or
                             (HttpStatusCode)449;
    }
}

public static class Log
{
    public static void Info(string header, string text)
    {
#if LINQPAD
        text.Dump(header);
#else
        Console.WriteLine($"{header}: {text}");
#endif
    }

    public static void Error(string header, string text)
    {
#if LINQPAD
        text.Dump(header);
#else
        Console.BackgroundColor = ConsoleColor.Red;
        Console.WriteLine($"{header}: {text}");
        Console.ResetColor();
#endif
    }
}

public class SimpleTimer : IDisposable
{
    private Stopwatch _sw;
    private string _name;

    private SimpleTimer(string name)
    {
        _name = name;
        _sw = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _sw.Stop();
#if LINQPAD
        _sw.Elapsed.Dump(_name);
#else
        Console.WriteLine($"{_name}: {_sw.Elapsed}");
#endif
    }

    public static SimpleTimer Show(string name)
    {
        return new SimpleTimer(name);
    }
}

public class SimpleProgress
{
    private string _header;
    private string _template;

#if LINQPAD
    private Util.ProgressBar progress;
#endif

    private SimpleProgress(string header, string template)
    {
        _header = header;
        _template = template;

#if LINQPAD
        progress = new Util.ProgressBar(header);
#endif
    }

    public void Update(params object[] args)
    {
        var arr = Array.ConvertAll(args, a => (object)a);
        var text = string.Format($"{_header}: {_template}", arr);

#if LINQPAD
        var percent = (double)((int)args[1]) / (double)((int)args[0]);
        progress.Caption = text;
        progress.Fraction = percent;
#else
        Console.Write($"\r{text}");
#endif
    }

    public static SimpleProgress StartTracking(string header, string template)
    {
        var progress = new SimpleProgress(header, template);

#if LINQPAD
        progress.progress.Dump();
#endif

        return progress;
    }
}
