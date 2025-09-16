//https://learn.microsoft.com/en-us/azure/cosmos-db/partial-document-update#supported-operations

#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: Microsoft.Azure.Cosmos, 3.35.4"
#r "nuget: Polly, 8.0.0"

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos;
using Polly;

var databaseName = "TruckTicketing";
var containerName = "Operations";
var propertyName = "WasteCode";

var q = new Queue<string>(Args);
q.TryDequeue(out var connectionString);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("The connection string must be provided.");
    return;
}

// cosmos connection
var client = new CosmosClient(connectionString);
var database = client.GetDatabase(databaseName);
var container = database.GetContainer(containerName);
var chunkSize = 1;

int counter = 0;
int total = 0;
int bounces = 0;
var policy = Policy.Handle<CosmosException>(e => e.StatusCode == HttpStatusCode.TooManyRequests).WaitAndRetryAsync(
        10,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        (e, t) => Interlocked.Increment(ref bounces));

// process all TTs
Console.WriteLine($"Started at: {DateTime.Now}");
await ProcessTruckTickets();
Console.WriteLine();
Console.WriteLine($"Finished at: {DateTime.Now}");

async Task ProcessTruckTickets()
{
    // get total items
    using var totalFeed = container.GetItemQueryIterator<JObject>("SELECT COUNT(1) as total FROM c WHERE c.EntityType = 'TruckTicket'");
    
    if (totalFeed.HasMoreResults)
    {
        var response = await totalFeed.ReadNextAsync();
        total = (int)response.First()["total"];
    }

    // get all items in question.
    var mainQuery = new QueryDefinition($"Select c.id, c.DocumentType, c.SubstanceName, c.WasteCode FROM c WHERE c.EntityType = 'TruckTicket' AND c.SubstanceName = 'Butane' AND c.WasteCode = 'Butane' ");
    var mainQueryOptions = new QueryRequestOptions()
    {
        MaxBufferedItemCount = 10000,
        MaxConcurrency = 10,
        MaxItemCount = 10000,
    };

    // iterate over all of them
    using var mainFeed = container.GetItemQueryIterator<JObject>(mainQuery, requestOptions: mainQueryOptions);
    while (mainFeed.HasMoreResults)
    {
        // fetch items
        var response = await policy.ExecuteAsync(async () => await mainFeed.ReadNextAsync());

        // process each item
        var tasks = response.Chunk(chunkSize).Select(UpdateTruckTicketBatch);
        await Task.WhenAll(tasks);
    }
}

async Task UpdateTruckTicketBatch(JObject[] jsonObjects)
{
    foreach (var jsonObject in jsonObjects)
    {
        await UpdateOneTruckTicket(jsonObject);
        Interlocked.Increment(ref counter);

        Console.Write($"\rTruck Tickets updated: {counter} of {total} (bounces: {bounces})");
    }
}

async Task UpdateOneTruckTicket(JObject jobj)
{
    string wasteCode = string.Empty; 

    await UpdateTheDocument((string)jobj["id"], (string)jobj["DocumentType"], wasteCode);
}

async Task UpdateTheDocument(string docId, string partitionKey, string propertyValue)
{
    await policy.ExecuteAsync(async () =>
    {
       var patchOps = new List<PatchOperation>
		{
            /*• If the target path specifies an element that already exists, its value is replaced.*/
			PatchOperation.Add($"/{propertyName}", propertyValue),
		};

		await container.PatchItemAsync<JObject>(docId, new PartitionKey(partitionKey), patchOps); 
           
    
        
    });
}