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

// substitute the 'Args' argument array when running in LINQPAD
#if LINQPAD
var Args = new List<string>
{
	Util.GetPassword("tt-dev-cosmos-conn"),
};
#endif

// ---=== MAIN SCRIPT ===---

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
var operationsContainer = database.GetContainer("Pricing");

// vars
int pricingRulesProcessed = 0;
int pricingRulesTotal = 0;
int bounces = 0;
bool isEdiFieldsEnabled = true;

// policies
var feedPolicy = ResiliencePipelines.SetupCosmosFeedResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var genericPolicy = ResiliencePipelines.SetupCosmosGenericResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var policySet = (feedPolicy, genericPolicy);

// progress
var progress = SimpleProgress.StartTracking("PricingRules", "updated {1} of {0} (bounces: {2})");

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
	var queryPattern = "SELECT {0} FROM c WHERE c.DocumentType = 'PricingRule' AND c.EntityType = 'PricingRule' AND NOT IS_NULL(c.ActiveTo) AND IS_DEFINED(c.ActiveTo) AND EndsWith(c.ActiveTo, 'T00:00:00+00:00')";
	var totalQuery = string.Format(queryPattern, "COUNT(1) as total");
	var mainQuery = string.Format(queryPattern, "c.id, c.Id, c.DocumentType, c.ActiveTo");

	// totals
	var totalDoc = await CosmosTools.QueryOne(operationsContainer, policySet, totalQuery, null);
	pricingRulesTotal = (int)totalDoc["total"];

	// main query
	await CosmosTools.QueryStreaming(operationsContainer, policySet, mainQuery, null, ProcessPricingRulesChunked, new(), new QueryRequestOptions
	{
		MaxBufferedItemCount = 1_000,
		MaxConcurrency = 25,
		MaxItemCount = 100,
	});

	async Task ProcessPricingRulesChunked(FeedResponse<JObject> batch)
	{
		var tasks = batch.Chunk(10).Select(ProcessPricingRules);
		await Task.WhenAll(tasks);
	}

	Log.Info(nameof(ProcessAll), $"Actual updates: {pricingRulesProcessed}");
}

async Task ProcessPricingRules(JObject[] pricingRules)
{
	foreach (var pricingRule in pricingRules)
	{
		await ProcessPricingRule(pricingRule);
	}
}

async Task ProcessPricingRule(JObject pricingRule)
{
	var pricingRuleActiveTo = (DateTimeOffset) pricingRule["ActiveTo"];
	pricingRuleActiveTo = pricingRuleActiveTo.ToUniversalTime().AddHours(23).AddMinutes(59).AddSeconds(59);

	Log.Info(nameof(ProcessPricingRule), $"pricingRuleActiveTo: {pricingRuleActiveTo}");

	// update the pricingRule's ActiveTo
	await PatchPricingRule((string)pricingRule["id"], (string)pricingRule["DocumentType"], pricingRuleActiveTo);

	// stats
	Interlocked.Increment(ref pricingRulesProcessed);
	progress.Update(pricingRulesTotal, pricingRulesProcessed, bounces);
}

async Task PatchPricingRule(string pricingRuleId, string partitionKey, DateTimeOffset propertyValue)
{
	Log.Info(nameof(PatchPricingRule), $"Updating: {pricingRuleId}/{partitionKey} to {propertyValue}");

	await CosmosTools.PatchProperty(operationsContainer, genericPolicy, pricingRuleId, partitionKey, $"/ActiveTo", propertyValue);
}
