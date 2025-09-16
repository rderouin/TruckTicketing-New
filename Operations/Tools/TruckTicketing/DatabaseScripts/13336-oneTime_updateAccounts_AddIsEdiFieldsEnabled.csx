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
var operationsContainer = database.GetContainer("Operations");

// vars
int accountsProcessed = 0;
int accountsTotal = 0;
int bounces = 0;
bool isEdiFieldsEnabled = true;

// policies
var feedPolicy = ResiliencePipelines.SetupCosmosFeedResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var genericPolicy = ResiliencePipelines.SetupCosmosGenericResiliencePipeline(arg => { bounces++; return ValueTask.CompletedTask; });
var policySet = (feedPolicy, genericPolicy);

// progress
var progress = SimpleProgress.StartTracking("Accounts", "updated {1} of {0} (bounces: {2})");

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
	var queryPattern = "SELECT {0} FROM c WHERE c.DocumentType = 'Accounts' AND c.EntityType = 'Account' AND NOT IS_DEFINED(c.IsEdiFieldsEnabled)";
	var totalQuery = string.Format(queryPattern, "COUNT(1) as total");
	var mainQuery = string.Format(queryPattern, "c.id, c.Id, c.DocumentType");

	// totals
	var totalDoc = await CosmosTools.QueryOne(operationsContainer, policySet, totalQuery, null);
	accountsTotal = (int)totalDoc["total"];

	// main query
	await CosmosTools.QueryStreaming(operationsContainer, policySet, mainQuery, null, ProcessAccountsChunked, new(), new QueryRequestOptions
	{
		MaxBufferedItemCount = 1_000,
		MaxConcurrency = 5,
		MaxItemCount = 100,
	});

	async Task ProcessAccountsChunked(FeedResponse<JObject> batch)
	{
		var tasks = batch.Chunk(10).Select(ProcessAccounts);
		await Task.WhenAll(tasks);
	}

	Log.Info(nameof(ProcessAll), $"Actual updates: {accountsProcessed}");
}

async Task ProcessAccounts(JObject[] accounts)
{
	foreach (var account in accounts)
	{
		await ProcessAccount(account);
	}
}

async Task ProcessAccount(JObject account)
{
	// update the account's isEdiFieldsEnabled
	await PatchAccount((string)account["id"], (string)account["DocumentType"], isEdiFieldsEnabled);

	// stats
	Interlocked.Increment(ref accountsProcessed);
	progress.Update(accountsTotal, accountsProcessed, bounces);
}

async Task PatchAccount(string accountId, string partitionKey, bool propertyValue)
{
	await CosmosTools.PatchProperty(operationsContainer, genericPolicy, accountId, partitionKey, $"/IsEdiFieldsEnabled", propertyValue);
}
