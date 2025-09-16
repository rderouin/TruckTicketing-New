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

#load "_dev_tools_se_.csx"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Retry;

////////////////////////////// CUT HERE //////////////////////////////

// ---=== MAIN SCRIPT ===---
var delete = true;

// init
var q = new Queue<string>(Args);
q.TryDequeue(out var connectionString);
if (string.IsNullOrWhiteSpace(connectionString))
{
	Log.Info("CommandLine", "The connection string must be provided.");
	return;
}

// cosmos connection
var client = new CosmosClient(connectionString);
var database = client.GetDatabase("TruckTicketing");
var operationsContainer = database.GetContainer("Operations");

// vars
int total = 0;
int processed = 0;
int bounces = 0;
var startTime = DateTimeOffset.UtcNow;

// policies
var feedPolicy = ResiliencePipelines.SetupCosmosFeedResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var genericPolicy = ResiliencePipelines.SetupCosmosGenericResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var policySet = (feedPolicy, genericPolicy);

// progress
var progress = SimpleProgress.StartTracking("ChangeTracking", "deleted {1} of {0} (bounces: {2}) => ETA: {3:d\\.hh\\:mm\\:ss}");

// kick off
Log.Info("General", $"{DateTime.Now}");
using (SimpleTimer.Show("Entire script"))
{
	await ProcessAll();
}
Log.Info("General", $"{DateTime.Now}");

// main method
async Task ProcessAll()
{
	var queryPattern = "SELECT {0} FROM c WHERE c.EntityType = 'ChangeTracking' ORDER BY c.DocumentType";
	var totalQuery = string.Format(queryPattern, "COUNT(1) as total");
	var mainQuery = string.Format(queryPattern, "c.id, c.DocumentType");

	// totals
	var totalDoc = await CosmosTools.QueryOne(operationsContainer, policySet, totalQuery, null);
	total = (int)totalDoc["total"];

	// main query
	var chunkSize = 1_000;
	var concurrencyFactor = Environment.ProcessorCount * 2;
	await CosmosTools.QueryStreaming(operationsContainer, policySet, mainQuery, null, ProcessInvoicesChunked, new(), new QueryRequestOptions
	{
		MaxBufferedItemCount = chunkSize * concurrencyFactor,
		MaxConcurrency = concurrencyFactor,
		MaxItemCount = chunkSize * concurrencyFactor,
	});

	async Task ProcessInvoicesChunked(FeedResponse<JObject> batch)
	{
		await Task.WhenAll(batch.Chunk(chunkSize).Select(ProcessChunk));
	}

	Log.Info(nameof(ProcessAll), $"Deleted: {processed}");
}

async Task ProcessChunk(JObject[] documents)
{
	// group documents by partition
	var partitionedDocuments = documents.ToLookup(d => (string)d["DocumentType"]);

	// process in chunks
	foreach (var singlePartitionDocuments in partitionedDocuments)
	{
		// create a transaction per partition
		var transaction = operationsContainer.CreateTransactionalBatch(new(singlePartitionDocuments.Key));

		// schedule to delete the block of documents
		foreach (var doc in singlePartitionDocuments)
		{
			transaction.DeleteItem((string)doc["id"]);
		}

		// execute the transaction
		if (delete)
		{
			await transaction.ExecuteAsync();
		}

		// update stats
		Interlocked.Add(ref processed, singlePartitionDocuments.Count());
		var elapsed = (DateTimeOffset.UtcNow - startTime).TotalSeconds;
		var eta = total * elapsed / processed;
		progress.Update(total, processed, bounces, TimeSpan.FromSeconds(eta));
	}
}
